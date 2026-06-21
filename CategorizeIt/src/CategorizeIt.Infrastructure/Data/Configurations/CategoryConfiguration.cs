using CategorizeIt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CategorizeIt.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        builder.Property(e => e.Color)
            .HasMaxLength(7);

        builder.HasData(
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "Food & Dining",         Icon = "restaurant",      Color = "#F59E0B", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "Transport",             Icon = "directions_car",  Color = "#3B82F6", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "Housing & Utilities",   Icon = "home",            Color = "#8B5CF6", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "Shopping",              Icon = "shopping_bag",    Color = "#EC4899", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000005"), Name = "Entertainment",         Icon = "movie",           Color = "#06B6D4", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000006"), Name = "Health",                Icon = "favorite",        Color = "#EF4444", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000007"), Name = "Education",             Icon = "school",          Color = "#10B981", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000008"), Name = "Income",                Icon = "trending_up",     Color = "#22C55E", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000009"), Name = "Subscriptions",         Icon = "subscriptions",   Color = "#F97316", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = new Guid("00000000-0000-0000-0000-000000000010"), Name = "Other",                 Icon = "more_horiz",      Color = "#6B7280", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}