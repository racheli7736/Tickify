using Microsoft.EntityFrameworkCore;
using Tickify.Core.Entities;

namespace Tickify.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
