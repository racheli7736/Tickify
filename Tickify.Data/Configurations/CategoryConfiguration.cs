using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tickify.Core.Entities;

namespace Tickify.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        // Case-insensitive uniqueness is enforced by a PostgreSQL expression index on lower("Name"),
        // created with raw SQL in the CaseInsensitiveCategoryNames migration (EF 8 cannot model it).
    }
}
