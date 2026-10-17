using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tickify.Data;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Creates the database if needed and applies the migrations (which include the seed data), so the system
    /// starts without any manual database step. Keeps the DbContext out of the API project.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
