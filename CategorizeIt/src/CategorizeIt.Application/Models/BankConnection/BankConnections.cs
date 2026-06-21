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

public class BankConnectionDto
{
    public Guid Id { get; set; }
    public string AspspName { get; set; } = string.Empty;
    public string AspspCountry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BankAccountDto> Accounts { get; set; } = new();
}

public record InitiateAuthResult(string Url, string State);