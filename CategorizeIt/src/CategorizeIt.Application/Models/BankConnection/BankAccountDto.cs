namespace CategorizeIt.Application.Models.BankConnections;

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? Name { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? CashAccountType { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}