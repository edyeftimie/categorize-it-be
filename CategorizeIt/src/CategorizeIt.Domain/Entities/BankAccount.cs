namespace CategorizeIt.Domain.Entities;

public class BankAccount
{
    public Guid Id { get; set; }
    public Guid BankConnectionId { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? Name { get; set; }
    public string Currency { get; set; } = "RON";
    public string? CashAccountType { get; set; }
    public string IdentificationHash { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BankConnection BankConnection { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}