using System.Text.Json.Serialization;

namespace CategorizeIt.Application.Models.EnableBanking;

public class StartAuthResponseDto
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("authorization_id")]
    public string AuthorizationId { get; set; } = string.Empty;

    [JsonPropertyName("psu_id_hash")]
    public string? PsuIdHash { get; set; }
}

public class CreateSessionResponseDto
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("accounts")]
    public List<SessionAccountDto> Accounts { get; set; } = new();

    [JsonPropertyName("aspsp")]
    public AspspRefDto Aspsp { get; set; } = new();

    [JsonPropertyName("psu_type")]
    public string PsuType { get; set; } = string.Empty;

    [JsonPropertyName("access")]
    public SessionAccessDto Access { get; set; } = new();
}

public class SessionAccountDto
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("identification_hash")]
    public string IdentificationHash { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("cash_account_type")]
    public string? CashAccountType { get; set; }
}

public class SessionAccessDto
{
    [JsonPropertyName("balances")]
    public bool Balances { get; set; }

    [JsonPropertyName("transactions")]
    public bool Transactions { get; set; }

    [JsonPropertyName("valid_until")]
    public DateTime ValidUntil { get; set; }
}

public class AspspRefDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}