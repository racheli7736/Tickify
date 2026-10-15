using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tickify.Core.Entities;

namespace Tickify.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.TotalPrice).HasPrecision(10, 2);
        builder.Property(o => o.SeatsSummary).IsRequired().HasMaxLength(500);
        builder.HasIndex(o => o.UserId);

        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Event)
            .WithMany(e => e.Orders)
            .HasForeignKey(o => o.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
