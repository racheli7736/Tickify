using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tickify.Tests.Integration;

/// <summary>
/// Hosts the real API in memory in the "Testing" environment: no User Secrets are loaded, so the development
/// database can never be reached; every setting the API needs is supplied here.
/// </summary>
public abstract class TestApiFactory : WebApplicationFactory<Program>
{
    public const string ReactOrigin = "http://localhost:5173";

    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "tickify-api-tests-" + Guid.NewGuid().ToString("N"));

    protected abstract string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_contentRoot);   // keeps the NLog files of the tests out of the project folder
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(_contentRoot);
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Jwt:Key", new string('t', 64));   // test-only dummy key, not a secret
        builder.UseSetting("Jwt:Issuer", "Tickify");
        builder.UseSetting("Jwt:Audience", "Tickify.Client");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("Cors:AllowedOrigins:0", ReactOrigin);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(_contentRoot, recursive: true); } catch (IOException) { /* log files may still be closing */ }
    }
}

/// <summary>The isolated PostgreSQL test database (database name ending with "_test").</summary>
public sealed class TickifyApiFactory : TestApiFactory
{
    protected override string ConnectionString => DatabaseFixture.RequireConnectionString();
}

/// <summary>A database that cannot be reached: lets a test provoke a real unhandled exception without any infrastructure.</summary>
public sealed class UnreachableDatabaseApiFactory : TestApiFactory
{
    protected override string ConnectionString =>
        "Host=localhost;Port=1;Database=unreachable_test;Username=nobody;Password=nothing;Timeout=2;Command Timeout=2";
}
