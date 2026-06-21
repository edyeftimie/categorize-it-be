using System.Text.Json.Serialization;

namespace CategorizeIt.Application.Models.EnableBanking;

public class AspspDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("psu_types")]
    public List<string> PsuTypes { get; set; } = new();

    [JsonPropertyName("auth_methods")]
    public List<AspspAuthMethod> AuthMethods { get; set; } = new();

    [JsonPropertyName("maximum_consent_validity")]
    public long? MaximumConsentValidity { get; set; }

    [JsonPropertyName("beta")]
    public bool Beta { get; set; }

    [JsonPropertyName("bic")]
    public string? Bic { get; set; }
}

public class AspspAuthMethod
{
    [JsonPropertyName("psu_type")]
    public string PsuType { get; set; } = string.Empty;

    [JsonPropertyName("approach")]
    public string Approach { get; set; } = string.Empty;

    [JsonPropertyName("hidden_method")]
    public bool HiddenMethod { get; set; }
}