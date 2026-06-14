namespace CategorizeIt.Application.Models.Transactions;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? EntryReference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsExpense { get; set; }
    public DateTime BookingDate { get; set; }
    public string? MerchantName { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
    public bool IsManual { get; set; }
    public bool? IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RON";
    public bool IsExpense { get; set; }
    public DateTime BookingDate { get; set; }
    public string? MerchantName { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
}

public class RecategoriseRequest
{
    public Guid? CategoryId { get; set; }
}