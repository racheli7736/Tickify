namespace Tickify.Tests.Integration;

/// <summary>
/// Integration test that needs a PostgreSQL test database. It is skipped (not failed) when the
/// TICKIFY_TEST_CONNECTION environment variable is not set, so `dotnet test` works on any machine.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(DatabaseFixture.ConnectionString))
            Skip = $"Set {DatabaseFixture.EnvVarName} to a PostgreSQL connection string whose database name ends with '_test'.";
    }
}
