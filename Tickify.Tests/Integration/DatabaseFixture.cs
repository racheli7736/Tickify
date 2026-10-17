using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tickify.Data;

namespace Tickify.Tests.Integration;

/// <summary>
/// Creates a dedicated PostgreSQL test database (migrations + seed data) and drops it at the end.
///
/// These tests REQUIRE PostgreSQL (the optimistic-concurrency token is PostgreSQL's xmin; an in-memory provider cannot
/// prove it). They are never skipped: when no test database is configured they FAIL with the setup instructions below.
///
/// Safety: the database name must end with "_test", so the development database ("tickify") can never be touched,
/// and nothing here reads the development connection string or User Secrets.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    /// <summary>Full connection string of the test database (overrides everything else).</summary>
    public const string EnvVarName = "TICKIFY_TEST_CONNECTION";

    /// <summary>Same variable that docker-compose.yml uses for the postgres password; with it the tests need no other setup.</summary>
    public const string PasswordEnvVarName = "TICKIFY_DB_PASSWORD";
    public const string PortEnvVarName = "TICKIFY_DB_PORT";
    public const string DefaultTestDatabase = "tickify_test";

    public const string SetupInstructions =
        "The PostgreSQL integration tests need a dedicated test database (its name must end with '_test'). Option A (PowerShell): " +
        "$env:TICKIFY_DB_PASSWORD = '<password>'; docker compose up -d; dotnet test   (uses localhost:5433, database tickify_test). " +
        "Option B: set TICKIFY_TEST_CONNECTION to a full connection string, e.g. " +
        "'Host=localhost;Port=5432;Database=tickify_test;Username=postgres;Password=<password>'. " +
        "To run only the tests that need no database: dotnet test --filter \"Category!=PostgreSQL\". See README, section Tests.";

    public static string? ConnectionString => Resolve();

    private static string? Resolve()
    {
        var explicitConnection = Environment.GetEnvironmentVariable(EnvVarName);
        if (!string.IsNullOrWhiteSpace(explicitConnection))
            return explicitConnection;

        // Convention that matches docker-compose.yml: the same server, but the separate database "tickify_test".
        var password = Environment.GetEnvironmentVariable(PasswordEnvVarName);
        if (string.IsNullOrWhiteSpace(password))
            return null;

        var port = int.TryParse(Environment.GetEnvironmentVariable(PortEnvVarName), out var p) ? p : 5433;
        return new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = port,
            Database = DefaultTestDatabase,
            Username = "postgres",
            Password = password
        }.ConnectionString;
    }

    /// <summary>The configured test connection, or a failure that explains how to configure it.</summary>
    public static string RequireConnectionString()
    {
        var connection = ConnectionString
            ?? throw new InvalidOperationException("No test database is configured. " + SetupInstructions);
        EnsureIsTestDatabase(connection);
        return connection;
    }

    public async Task InitializeAsync()
    {
        var connection = RequireConnectionString();

        try
        {
            await using var db = CreateContext(connection);
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(
                $"Could not prepare the test database: {ex.Message} {SetupInstructions}", ex);
        }
    }

    public async Task DisposeAsync()
    {
        var connection = ConnectionString;
        if (connection is null)
            return; // initialization already failed with the instructions

        EnsureIsTestDatabase(connection);
        try
        {
            await using var db = CreateContext(connection);
            await db.Database.EnsureDeletedAsync();
        }
        catch (NpgsqlException)
        {
            // PostgreSQL was not reachable in the first place; InitializeAsync reported that.
        }
    }

    public AppDbContext CreateContext() => CreateContext(RequireConnectionString());

    private static AppDbContext CreateContext(string connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

    /// <summary>Back to the freshly seeded state: all seats available, no orders.</summary>
    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Seats\" SET \"Status\" = 'Available', \"OrderId\" = NULL; DELETE FROM \"Orders\";");
    }

    public static void EnsureIsTestDatabase(string connectionString)
    {
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (string.IsNullOrWhiteSpace(database) || !database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to run integration tests: database '{database}' does not end with '_test'. " +
                "This protects the development database from being dropped.");
    }
}

[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Integration";
}
