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

In the Development environment the API applies the migrations and the seed data automatically on startup.
To apply them manually: `dotnet ef database update -p Tickify.Data -s Tickify.API`.

Demo users: `admin@tickets.local` / `Admin123!` and `user@tickets.local` / `User123!`.

### Concurrency on PostgreSQL

`Seat.RowVersion` is a `uint` mapped to PostgreSQL's `xmin` system column, which the database changes on every
update of the row. EF Core adds it to the `WHERE` clause of the seat `UPDATE`, so when two buyers race for the same
seat, the second `SaveChanges` throws `DbUpdateConcurrencyException` (translated to a 409 by the service layer).

### Notes

- Timestamps are `timestamptz`; the API only writes UTC values.
- Event search uses `ILIKE` (case-insensitive); category names are unique case-sensitively.
- The seed migration advances the identity sequences after inserting rows with explicit ids.
