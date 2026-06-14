using CategorizeIt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CategorizeIt.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(e => e.EntryReference)
            .HasMaxLength(256);

        builder.Property(e => e.MerchantName)
            .HasMaxLength(200);

        builder.Property(e => e.MerchantCategoryCode)
            .HasMaxLength(10);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasIndex(e => e.EntryReference);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.BankAccount)
            .WithMany(a => a.Transactions)
            .HasForeignKey(e => e.BankAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}