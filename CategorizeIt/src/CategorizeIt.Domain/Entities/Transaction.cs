namespace CategorizeIt.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? EntryReference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RON";
    public bool IsExpense { get; set; }
    public DateTime BookingDate { get; set; }
    public string? MerchantName { get; set; }
    public string? MerchantCategoryCode { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsManual { get; set; }
    public bool? IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public BankAccount? BankAccount { get; set; }
    public Category? Category { get; set; }
}