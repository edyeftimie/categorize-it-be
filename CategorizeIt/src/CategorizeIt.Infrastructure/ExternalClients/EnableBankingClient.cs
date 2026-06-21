using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.EnableBanking;
using CategorizeIt.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CategorizeIt.Infrastructure.ExternalClients;

public class EnableBankingClient : IEnableBankingClient
{
    private readonly HttpClient _httpClient;
    private readonly EnableBankingSettings _settings;
    private readonly RsaSecurityKey _signingKey;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EnableBankingClient(HttpClient httpClient, IOptions<EnableBankingSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;

        var rsa = RSA.Create();
        rsa.ImportFromPem(_settings.PrivateKey);
        _signingKey = new RsaSecurityKey(rsa) { KeyId = _settings.ApplicationId };
    }

    public async Task<IReadOnlyList<AspspDto>> GetAspspsAsync(string country, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/aspsps?country={country}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt());

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Enable Banking /aspsps failed with {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var wrapper = JsonSerializer.Deserialize<AspspsResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Enable Banking returned an empty response for /aspsps");

        return wrapper.Aspsps;
    }

    private string GenerateJwt()
    {
        var now = DateTimeOffset.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "enablebanking.com",
            Audience = "api.enablebanking.com",
            IssuedAt = now.UtcDateTime,
            Expires = now.AddSeconds(3600).UtcDateTime,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return handler.WriteToken(handler.CreateJwtSecurityToken(descriptor));
    }

    private class AspspsResponse
    {
        [JsonPropertyName("aspsps")]
        public List<AspspDto> Aspsps { get; set; } = new();
    }

    public async Task<TransactionsPageDto> GetAccountTransactionsAsync(string accountUid, string? dateFrom, string? continuationKey, CancellationToken ct = default)
    {
        var query = continuationKey != null
            ? $"continuation_key={Uri.EscapeDataString(continuationKey)}"
            : dateFrom != null
                ? $"date_from={dateFrom}"
                : "strategy=longest";

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_settings.BaseUrl}/accounts/{Uri.EscapeDataString(accountUid)}/transactions?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt());

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Enable Banking /accounts/{accountUid}/transactions failed with {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TransactionsPageDto>(json, _jsonOptions)
            ?? throw new InvalidOperationException($"Empty response for account {accountUid} transactions");
    }

    public async Task<StartAuthResponseDto> StartAuthorizationAsync(
        string aspspName,
        string aspspCountry,
        string redirectUrl,
        string state,
        DateTimeOffset validUntil,
        CancellationToken ct = default)
    {
        var body = new
        {
            access = new { valid_until = validUntil.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ") },
            aspsp = new { name = aspspName, country = aspspCountry },
            state = state,
            redirect_url = redirectUrl,
            psu_type = "personal"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/auth");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt());
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Enable Banking /auth failed with {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<StartAuthResponseDto>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Empty response from /auth");
    }

    public async Task<CreateSessionResponseDto> CreateSessionAsync(
        string code,
        CancellationToken ct = default)
    {
        var body = new { code = code };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt());
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Enable Banking /sessions failed with {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<CreateSessionResponseDto>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Empty response from /sessions");
    }
}