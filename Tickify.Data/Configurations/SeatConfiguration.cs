using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tickify.Core.Entities;

namespace Tickify.Data.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Optimistic concurrency token: the heart of the "two buyers, one seat" protection.
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasIndex(s => new { s.EventId, s.Row, s.Number }).IsUnique();

        builder.HasOne(s => s.Event)
            .WithMany(e => e.Seats)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Order)
            .WithMany(o => o.Seats)
            .HasForeignKey(s => s.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
