using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tickify.Data;

namespace Tickify.Tests.Integration;

/// <summary>
/// Creates a dedicated PostgreSQL test database (migrations + seed data) and drops it at the end.
/// Safety guard: the database name must end with "_test", so these tests can never touch the
/// development database (which is simply called "tickify").
/// The connection string comes from an environment variable and is never stored in the repository, e.g.
///   Host=localhost;Port=5433;Database=tickify_test;Username=postgres;Password=...
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public const string EnvVarName = "TICKIFY_TEST_CONNECTION";

    public static string? ConnectionString => Environment.GetEnvironmentVariable(EnvVarName);

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return; // every test is skipped via PostgresFact

        EnsureIsTestDatabase(ConnectionString);

        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return;

        EnsureIsTestDatabase(ConnectionString);
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Back to the freshly seeded state: all seats available, no orders.</summary>
    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Seats\" SET \"Status\" = 'Available', \"OrderId\" = NULL; DELETE FROM \"Orders\";");
    }

    private static void EnsureIsTestDatabase(string connectionString)
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
