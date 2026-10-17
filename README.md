# Tickify

Event ticket sales system - ASP.NET Core Web API (.NET 8) with EF Core and **PostgreSQL**.
(The full README is written in the final stage; this section covers the database setup.)

## Database: PostgreSQL

The server uses PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL` (EF Core 8).

### 1. Start PostgreSQL locally

With Docker (port 5433 is used so it does not clash with another local PostgreSQL on 5432):

```powershell
$env:TICKIFY_DB_PASSWORD = "<choose a password>"
docker compose up -d
```

Or use any PostgreSQL 14+ installation and create an empty database named `tickify`.

### 2. Store the connection string and JWT key in User Secrets (never in the repo)

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=tickify;Username=postgres;Password=<your password>" --project Tickify.API
dotnet user-secrets set "Jwt:Key" "<random string, 64+ characters>" --project Tickify.API
```

### 3. Run

```powershell
dotnet run --project Tickify.API
```

Swagger UI (Development only): `/swagger`. Log in with `POST /api/auth/login`, press **Authorize** and paste the token
to call the protected endpoints.

In the Development environment the API applies the migrations and the seed data automatically on startup.
To apply them manually: `dotnet ef database update -p Tickify.Data -s Tickify.API`.

Demo users: `admin@tickets.local` / `Admin123!` and `user@tickets.local` / `User123!`.

### Concurrency on PostgreSQL

`Seat.RowVersion` is a `uint` mapped to PostgreSQL's `xmin` system column, which the database changes on every
update of the row. EF Core adds it to the `WHERE` clause of the seat `UPDATE`, so when two buyers race for the same
seat, the second `SaveChanges` throws `DbUpdateConcurrencyException` (translated to a 409 by the service layer).

### Notes

- Timestamps are `timestamptz`; the API only writes UTC values.
- Event search uses `ILIKE` (case-insensitive). Category names are unique ignoring case, enforced by a unique index on `lower("Name")`; the migration refuses to run (without touching any data) if such duplicates already exist.
- Mapping between entities and DTOs uses AutoMapper 15.1.3 profiles (`Tickify.Service/Mapping`). Versions below 15.1.1 are affected by
  GHSA-rvv3-g6hj-g44x (DoS), so 15.1.1 or later is required. See [AutoMapper license](#automapper-license).
- The seed migration advances the identity sequences after inserting rows with explicit ids.

## Logging

NLog is the only logging provider; the code uses `ILogger<T>`. Every request gets a correlation id
(`X-Correlation-Id` header, or a new Guid) that is returned in the response, included in every log line and in
every error body (`correlationId`).

- Console: readable text. Files in `Tickify.API/logs/`: `app.log` (everything) and `errors.log` (errors), one JSON object per line.
- Files roll every day and at 10 MB into `logs/archive`; archives older than 14 days are deleted. The folder is git-ignored.
- Levels come from `Logging:LogLevel` in `appsettings`; seat conflicts and failed logins are `Warning`, unhandled exceptions are `Error`.
- Passwords, tokens, connection strings and e-mail addresses are never logged.

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

Official sources: [Pricing and Community License](https://automapper.io/#pricing) |
[Licensing FAQ](https://luckypennysoftware.com/faq) | [License agreement (PDF)](https://luckypennysoftware.com/license) |
[Setting the key](https://docs.automapper.io/en/stable/Getting-started.html) |
[RPL-1.5](https://opensource.org/license/rpl-1-5/)

This section is a summary, not legal advice.

## Tests

```powershell
dotnet test
```

Unit tests always run. The PostgreSQL integration tests (two `DbContext`s racing for the same seat, two concurrent
purchases through `OrderService`, case-insensitive category names) are **skipped** unless you provide a connection
string to a dedicated test database:

```powershell
$env:TICKIFY_TEST_CONNECTION = "Host=localhost;Port=5433;Database=tickify_test;Username=postgres;Password=<your password>"
dotnet test
```

Safety: the database name must end with `_test`, otherwise the tests refuse to run. The test database is dropped and
re-created (migrations + seed data) at the start and dropped at the end, so it never affects the development database.
