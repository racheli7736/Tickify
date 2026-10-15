using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tickify.Core.Entities;

namespace Tickify.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Venue).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TicketPrice).HasPrecision(10, 2);
        builder.HasIndex(e => e.StartsAt);
    }
}
