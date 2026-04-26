using CategorizeIt.Domain.Enums;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CategorizeIt.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(AppConstants.Validation.EmailMaxLength);

        builder.Property(e => e.Role)
            .HasConversion<string>();

        builder.Property(e => e.Username)
            .HasMaxLength(AppConstants.Validation.NameMaxLength);
    }
}