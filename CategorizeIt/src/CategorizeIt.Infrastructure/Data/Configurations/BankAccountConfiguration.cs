using CategorizeIt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CategorizeIt.Infrastructure.Data.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Uid)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Iban)
            .HasMaxLength(34);

        builder.Property(e => e.Name)
            .HasMaxLength(200);

        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(e => e.CashAccountType)
            .HasMaxLength(50);

        builder.Property(e => e.IdentificationHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(e => e.IdentificationHash)
            .IsUnique();

        builder.HasMany(e => e.Transactions)
            .WithOne(t => t.BankAccount)
            .HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}