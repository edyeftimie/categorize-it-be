using CategorizeIt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CategorizeIt.Infrastructure.Data.Configurations;

public class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.AspspName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AspspCountry)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(e => e.PsuType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.BankAccounts)
            .WithOne(a => a.BankConnection)
            .HasForeignKey(a => a.BankConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}