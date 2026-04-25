using CategorizeIt.Domain.Enums;

namespace CategorizeIt.Domain.Enums;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public Role Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? GoogleId { get; set; }
}