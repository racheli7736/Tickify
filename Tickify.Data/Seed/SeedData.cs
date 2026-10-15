using Microsoft.EntityFrameworkCore;
using Tickify.Core.Entities;
using Tickify.Core.Enums;

namespace Tickify.Data.Seed;

/// <summary>
/// Fixed ids, fixed dates and precomputed BCrypt hashes, so the model snapshot is
/// deterministic and does not produce a new migration on every build.
/// Demo credentials: admin@tickets.local / Admin123!  and  user@tickets.local / User123!
/// </summary>
public static class SeedData
{
    private const string AdminHash = "$2a$11$/c.bEkn0q5DhZ95twfl9ZuP5JUEMoVSovmr5xw2k6XYDI2HtHT7cC";
    private const string UserHash = "$2a$11$gQWX5BeMMEcK2a9LxUloWe3xNioxKRB6NZEXPYRuhQoy/daYNRCOa";

    private static readonly DateTime SeededAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Music" },
            new Category { Id = 2, Name = "Theater" },
            new Category { Id = 3, Name = "Comedy" },
            new Category { Id = 4, Name = "Sports" },
            new Category { Id = 5, Name = "Festival" });

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, FullName = "Admin", Email = "admin@tickets.local", PasswordHash = AdminHash, Role = UserRole.Admin, CreatedAt = SeededAt },
            new User { Id = 2, FullName = "Demo User", Email = "user@tickets.local", PasswordHash = UserHash, Role = UserRole.User, CreatedAt = SeededAt });

        var events = new[]
        {
            (Ev: new Event { Id = 1, Name = "Summer Rock Night", Description = "An open-air rock concert.", Venue = "City Arena", StartsAt = new DateTime(2028, 6, 15, 18, 0, 0, DateTimeKind.Utc), TicketPrice = 120m, CreatedAt = SeededAt }, Rows: 5, PerRow: 10, Cats: new[] { 1, 5 }),
            (Ev: new Event { Id = 2, Name = "Hamlet", Description = "Classic theater production.", Venue = "National Theater", StartsAt = new DateTime(2028, 7, 1, 20, 0, 0, DateTimeKind.Utc), TicketPrice = 90m, CreatedAt = SeededAt }, Rows: 4, PerRow: 8, Cats: new[] { 2 }),
            (Ev: new Event { Id = 3, Name = "Stand-Up Evening", Description = "Three comedians, one stage.", Venue = "Comedy Club", StartsAt = new DateTime(2028, 8, 10, 21, 0, 0, DateTimeKind.Utc), TicketPrice = 60m, CreatedAt = SeededAt }, Rows: 6, PerRow: 10, Cats: new[] { 3 }),
        };

        var seats = new List<Seat>();
        var eventCategories = new List<EventCategory>();
        var seatId = 1;
        foreach (var (ev, rows, perRow, cats) in events)
        {
            modelBuilder.Entity<Event>().HasData(ev);
            eventCategories.AddRange(cats.Select(c => new EventCategory { EventId = ev.Id, CategoryId = c }));
            for (var r = 1; r <= rows; r++)
                for (var n = 1; n <= perRow; n++)
                    seats.Add(new Seat { Id = seatId++, EventId = ev.Id, Row = r, Number = n, Status = SeatStatus.Available });
        }

        modelBuilder.Entity<EventCategory>().HasData(eventCategories);
        modelBuilder.Entity<Seat>().HasData(seats);
    }
}
