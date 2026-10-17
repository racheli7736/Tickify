# Tickify

Tickify is an event ticket sales system. Users browse upcoming events, pick specific seats on a seat map and buy up to
6 seats in one order; admins create events (rows x seats per row are generated automatically), assign them to
categories and see all orders. The server is an **ASP.NET Core Web API (.NET 8)** with **EF Core + PostgreSQL**,
JWT authentication and NLog. The React client is a separate repository.

## The limited resource: the seat

**Resource:** `Seat`. A seat can be sold only once, even when several users try to buy it at the same moment.

**Where the race is:** two buyers choose the same seat and press "buy" together. Both requests read the seat as
`Available`, both pass the business check, and without protection both would save an order for it.

**How the system protects it (optimistic concurrency):**
- `Seat.RowVersion` (`uint`, mapped to PostgreSQL's `xmin` system column) changes on every update of the row. EF Core adds
  the value that was read to the `WHERE` of the seat `UPDATE`, so the second writer finds no row and EF throws
  `DbUpdateConcurrencyException`.
- `OrderService.PurchaseAsync` validates and saves in one go: all checks, then **one** `SaveChangesAsync` (order + all
  seats in one transaction) - all or nothing.
- `UnitOfWork` catches only `DbUpdateConcurrencyException` and throws the domain `ConcurrencyConflictException`
  (the Service layer never depends on EF Core). The service logs a **Warning** (event, seats, user) and returns
  `Conflict`; the API answers **HTTP 409** with the message
  "אחד או יותר מהמושבים שבחרת נתפסו על ידי משתמש אחר. אנא בחרי מושבים מחדש."

**Proof (automated):** `Tickify.Tests/Integration/ConcurrencyTests.cs` - two separate `DbContext`s update the same seat (the
second save fails), and two `OrderService`s buy the same seat at once (one `Success`, one `Conflict`, one order in the
database). `Services/OrderServiceTests.cs` covers the accepted case, the sold-seat case and the conflict-on-save case with mocks.

## Architecture

```
Tickify.API  ----->  Tickify.Service  ----->  Tickify.Core   <-----  Tickify.Data
 controllers,         business rules,          entities, enums,       DbContext, Fluent API, repositories,
 middleware,          AutoMapper profiles,     DTOs, interfaces,      UnitOfWork, migrations, seed
 JWT, Swagger,        JWT + BCrypt             ServiceResult,
 DI, CORS                                      domain exceptions
 (API also references Data - only to register it in DI)
```

| Project | Responsibility | References |
|---|---|---|
| `Tickify.Core` | Entities, enums, DTOs (separate request/response), repository + service interfaces, `ServiceResult`, domain exceptions. **No NuGet packages.** | nothing |
| `Tickify.Data` | `AppDbContext`, Fluent API, repositories (`AsNoTracking` for reads, paging inside the query), `UnitOfWork`, migrations, seed | Core |
| `Tickify.Service` | All business rules (the 14 rules below), AutoMapper profiles, `JwtTokenService`, `BCryptPasswordHasher` | Core |
| `Tickify.API` | Thin controllers (no DbContext, no business logic), middleware, authentication/authorization, Swagger, DI composition root | Service, Core, Data |
| `Tickify.Tests` | xUnit + Moq unit tests, PostgreSQL integration tests, in-memory API tests | all |

Services return `ServiceResult` (`Validation / NotFound / Conflict / Forbidden / Unauthorized`); `ApiControllerBase` maps
them to 400 / 404 / 409 / 403 / 401. Everything is async with `CancellationToken`. Entities never leave the API - only DTOs.

### Request pipeline (the order matters)

`ExceptionHandlingMiddleware` (first: any unhandled exception becomes the uniform 500 JSON and an `Error` log line) ->
`CorrelationIdMiddleware` (id from `X-Correlation-Id` or a new Guid, put in the log scope, one log line per request) ->
status pages -> Swagger (Development) -> HTTPS redirection -> CORS -> **Authentication** -> **Authorization** -> controllers.

### Dependency injection and lifetimes

Every dependency is injected through the constructor; the interfaces live in `Core`, the implementations in `Data`/`Service`,
and everything is registered in `Tickify.API/Extensions/ServiceCollectionExtensions.cs` (called from `Program.cs`).

| Lifetime | One instance per... | Used here for |
|---|---|---|
| **Scoped** | HTTP request | `AppDbContext`, all repositories, `IUnitOfWork`, all services - they work on the request's `DbContext` |
| **Singleton** | application | `IPasswordHasher`, `ITokenService` - stateless, no scoped dependency |
| **Transient** | each injection | not needed |

**Why a singleton must not hold a scoped service (the "captive dependency" bug):** a singleton lives as long as the application, so
whatever it receives in its constructor lives that long too. A singleton that took the scoped `DbContext` would keep the *first*
request's `DbContext` forever: it is not thread-safe (concurrent requests would corrupt its state), its change tracker would keep growing
and serve stale data, and it would be used after its request ended (and may already be disposed). That is why the services that use
the `DbContext` are scoped, and only the stateless hasher and token service are singletons. In Development, ASP.NET Core validates this
on startup (`ValidateScopes`) and fails fast if a singleton captures a scoped service.

## Getting started

Requirements: **.NET 8 SDK**, **PostgreSQL 14+** (Docker is the easiest), Git.

### 1. Start PostgreSQL

With Docker (port 5433, so it does not clash with another PostgreSQL on 5432):

```powershell
$env:TICKIFY_DB_PASSWORD = "<choose a password>"
docker compose up -d
```

Or use any PostgreSQL and create an empty database named `tickify`.

### 2. Store the connection string and the JWT key in User Secrets (never in the repo)

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=tickify;Username=postgres;Password=<your password>" --project Tickify.API
dotnet user-secrets set "Jwt:Key" "<random string, 64+ characters>" --project Tickify.API
```

The API refuses to start with a clear message if either value is missing.

### 3. Trust the HTTPS development certificate (once)

```powershell
dotnet dev-certs https --trust
```

### 4. Run

```powershell
dotnet run --project Tickify.API --launch-profile https
```

- API: `https://localhost:7003`
- Swagger UI (Development only): `https://localhost:7003/swagger` - log in with `POST /api/auth/login`, press **Authorize**
  and paste the token (no `Bearer ` prefix).
- In Development the API applies the migrations and the seed data automatically on startup - no manual database step.
  (Manually: `dotnet ef database update -p Tickify.Data -s Tickify.API`.)

**Demo users** (created by the seed):

| Role | E-mail | Password |
|---|---|---|
| Admin | `admin@tickets.local` | `Admin123!` |
| User | `user@tickets.local` | `User123!` |

Every user created through `POST /api/auth/register` gets the role **User**.

The `https` launch profile listens **only** on HTTPS on purpose: an extra HTTP address would redirect to HTTPS, and a browser
cannot follow a redirect on a CORS preflight request, which breaks the React client. (The `http` profile is HTTP-only.)

## API endpoints

All endpoints except register/login require `Authorization: Bearer <token>`.

| Method and path | Who | Success | Other statuses |
|---|---|---|---|
| `POST /api/auth/register` | anyone | 201 + `Location: /api/users/{id}` (and a JWT) | 400, 409 (e-mail taken) |
| `POST /api/auth/login` | anyone | 200 | 400, 401 |
| `GET /api/events?categoryId&search&sortBy=date\|price\|name&sortDir=asc\|desc&page&pageSize(1-50)` | User (upcoming only), Admin (all) | 200 paged | 400, 401 |
| `GET /api/events/{id}` | any logged-in | 200 (incl. rows, seats per row, available seats) | 404 |
| `GET /api/events/{id}/seats` | any logged-in | 200 seat map (`Available` / `Sold`) | 404 |
| `POST /api/events` | Admin | 201 + `Location` (creates all seats) | 400, 403 |
| `PUT /api/events/{id}` | Admin | 204 (details + categories, not the seat structure) | 400, 403, 404 |
| `DELETE /api/events/{id}` | Admin | 204 | 403, 404, 409 (has sold tickets / orders) |
| `GET /api/users/{id}` | the user themself, or Admin | 200 (`id`, `fullName`, `role`, `createdAt` - no e-mail) | 403, 404 |
| `GET /api/categories` | any logged-in | 200 | 401 |
| `GET /api/categories/{id}` | any logged-in | 200 | 404 |
| `POST /api/categories` | Admin | 201 + `Location` | 400, 403, 409 (name taken, ignoring case) |
| `PUT /api/categories/{id}` | Admin | 204 | 400, 403, 404, 409 |
| `DELETE /api/categories/{id}` | Admin | 204 | 403, 404, 409 (assigned to events) |
| `POST /api/orders` | User | 201 + `Location` (1-6 seats, all or nothing) | 400, 403, 404, **409 (seat taken)** |
| `GET /api/orders?page&pageSize` | User (own), Admin (all, with purchaser id + name) | 200 paged | 400 |
| `GET /api/orders/{id}` | owner or Admin | 200 | 403, 404 |
| `DELETE /api/orders/{id}` | owner (User) | 204 (seats become available again) | 400, 403, 404, 409 |

Every `201 Created` carries a `Location` header that points to the created resource. Order responses contain the purchaser's `userId` and `userFullName` (so an admin can tell orders apart) but never an e-mail address.

**Error format:** every error (validation, business, 401/403/404, 500) is the same JSON -
`{ "status", "title", "detail", "correlationId" }` (RFC 7807 problem details; validation errors add `errors`).
Enums travel as strings (`"Admin"`, `"Available"`), dates are UTC ISO-8601.

## Business rules (all enforced in the Service layer)

1. An order has 1-6 seats (400). 2. No duplicate seat in an order (400). 3. All seats belong to the order's event (400).
4. The event exists (404). 5. No purchase for an event that already started (400). 6. All seats are available when read (409).
7. A seat taken between the read and the save (`DbUpdateConcurrencyException`) is a 409. 8. Only the owner can cancel (403).
9. Only an active order of a future event can be cancelled (400). 10. An event with sold tickets or orders cannot be deleted (409).
11. The seat structure is fixed at creation (no such fields in the update DTO). 12. `TotalPrice` is stored at purchase time and
never changes with the ticket price. 13. E-mail is unique (409). 14. Category names are unique ignoring case; a category assigned
to events cannot be deleted (409).

## Configuration

| Key | Where | Notes |
|---|---|---|
| `ConnectionStrings:Default` | User Secrets | PostgreSQL connection string |
| `Jwt:Key` | User Secrets | at least 64 characters |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `appsettings.json` | defaults: `Tickify`, `Tickify.Client`, 60 |
| `Cors:AllowedOrigins` | `appsettings.Development.json` | `http://localhost:5173`, `http://127.0.0.1:5173` (Vite); add more if your dev server uses another port |
| `Logging:LogLevel` | `appsettings*.json` | minimum levels; NLog only routes |

## For the React client

- Base URL: `https://localhost:7003` (after `dotnet dev-certs https --trust`). Send `Authorization: Bearer <token>`; the token lives 60 minutes (`expiresAt` is in the login response).
- CORS allows the origins above, any header and any method, and exposes `X-Correlation-Id` so the client can read it (it is also in every error body).
- On `409` from `POST /api/orders`, show the `detail` message and reload the seat map (`GET /api/events/{id}/seats`).

## Database

PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL` (EF Core 8), Code-First with three migrations (`InitialCreate`,
`SeedData`, `CaseInsensitiveCategoryNames`).

- Timestamps are `timestamptz`; the API writes UTC only. Event search uses `ILIKE` (case-insensitive).
- Category names are unique ignoring case through a unique index on `lower("Name")`; its migration refuses to run (without touching
  any data) if such duplicates already exist.
- A unique-constraint violation caused by a race (two simultaneous registrations with the same e-mail, or two categories with the
  same name) is translated by `UnitOfWork` into `DuplicateKeyException` and answered with **409**, never 500.
- The seed migration advances the identity sequences after inserting rows with explicit ids.

## Logging

NLog is the only logging provider; the code uses `ILogger<T>`. Every request gets a correlation id
(`X-Correlation-Id` header, or a new Guid) that is returned in the response, included in every log line and in
every error body (`correlationId`).

- Console: readable text. Files in `Tickify.API/logs/`: `app.log` (everything) and `errors.log` (errors), one JSON object per line.
- Files roll every day and at 10 MB into `logs/archive`; archives older than 14 days are deleted. The folder is git-ignored.
- Levels come from `Logging:LogLevel`; seat conflicts and failed logins are `Warning`, unhandled exceptions are `Error`.
- Passwords, tokens, connection strings and e-mail addresses are never logged.

## Tests

**116 tests**: xUnit + Moq unit tests of the Service layer, architecture guards, and PostgreSQL integration tests.

**Tests that need external infrastructure: 20 tests need a PostgreSQL database** (they are tagged `Category=PostgreSQL`):
`ConcurrencyTests` (including the required two-`DbContext` proof), `DuplicateKeyTests` and `ApiTests`. A real PostgreSQL is
required because the concurrency token is PostgreSQL's `xmin`; an in-memory provider cannot prove it. The other 96 tests need nothing.

**These tests are never skipped.** Without a configured test database they **fail with the setup instructions**, so a green
`dotnet test` always means the concurrency proof really ran.

### Run everything (PowerShell)

```powershell
$env:TICKIFY_DB_PASSWORD = "<choose a password>"   # the same variable that docker-compose.yml uses
docker compose up -d                               # PostgreSQL on localhost:5433 (skip if it is already running)
dotnet test
```

The tests connect to `localhost:5433` as user `postgres` and use the **separate database `tickify_test`**; `TICKIFY_DB_PORT` changes the port.
To use another server, set the full connection string instead:

```powershell
$env:TICKIFY_TEST_CONNECTION = "Host=localhost;Port=5432;Database=tickify_test;Username=postgres;Password=<your password>"
dotnet test
```

(bash: `export TICKIFY_DB_PASSWORD='...'` / `export TICKIFY_TEST_CONNECTION='...'`.)

### Run only the tests that need no database

```powershell
dotnet test --filter "Category!=PostgreSQL"
```

### Safety: the development database is never touched

- The database name must end with `_test`, otherwise every test refuses to run - a connection string that points at `tickify` is rejected before anything is executed.
- The tests never read the development connection string or User Secrets. The in-memory API tests run in a `Testing` environment (no User Secrets) with their own settings.
- The test database is dropped and re-created (migrations + seed data) at the start and dropped at the end.

### What is covered

The seat race (two `DbContext`s, two `OrderService`s), duplicate e-mail / category races (service level and over HTTP), 401 / 403 rules,
CORS, `Location` headers, the unhandled-exception pipeline (uniform 500 with correlation id), the admin orders data, the Service-layer
business rules with mocks, and architecture guards (Core has no packages, controllers only use service interfaces, exception handling is
the first middleware, every async interface method takes a `CancellationToken`).

## AutoMapper license

AutoMapper 15+ (Lucky Penny Software) is no longer MIT-licensed. Current terms (checked on 2026-10-17; they may change):

- **Free for this project.** The *Community License* is free and self-service. Individuals can use it, and the EULA explicitly lets
  "individual students ... use the Licensed Product under the Community License for genuine educational purposes - including ...
  coursework" (EULA section 4.2.g.3.3). Tickify is a non-commercial educational project, which fits this license.
  The source is also public under the Reciprocal Public License 1.5, a free alternative that requires sharing the source of software
  you distribute or deploy that uses it; this repository is public on GitHub.
- **Exception to check:** a student is not eligible if a contract with their university gives the university ownership of the work
  (EULA section 4.2.g.7).
- **License key:** only needed for production deployments. Development, testing and CI need no key. Without a key AutoMapper writes a
  `WARN` line at startup (about the Lucky Penny license); it is only a log message and nothing is limited. If the API is ever deployed
  for real users, register a free Community key and set it with `cfg.LicenseKey` in `AddAutoMapper`.
- Versions below 15.1.1 are affected by GHSA-rvv3-g6hj-g44x (DoS), so 15.1.1 or later is required (15.1.3 is used).

Official sources: [Pricing and Community License](https://automapper.io/#pricing) |
[Licensing FAQ](https://luckypennysoftware.com/faq) | [License agreement (PDF)](https://luckypennysoftware.com/license) |
[Setting the key](https://docs.automapper.io/en/stable/Getting-started.html) |
[RPL-1.5](https://opensource.org/license/rpl-1-5/)

This section is a summary, not legal advice.
