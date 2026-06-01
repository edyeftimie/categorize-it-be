namespace CategorizeIt.Application.Models.Authentication;

public class GoogleUserModel
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
}