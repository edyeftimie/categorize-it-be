namespace CategorizeIt.Application.Models.BankConnections;

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