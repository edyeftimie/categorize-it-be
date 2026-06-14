namespace CategorizeIt.Domain.Entities;

public class BankConnection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string AspspName { get; set; } = string.Empty;
    public string AspspCountry { get; set; } = "RO";
    public string PsuType { get; set; } = "personal";
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
}