using CategorizeIt.Application.Models.EnableBanking;

namespace CategorizeIt.Application.Interfaces;

public interface IEnableBankingClient
{
    Task<IReadOnlyList<AspspDto>> GetAspspsAsync(
        string country,
        CancellationToken ct = default);

    Task<TransactionsPageDto> GetAccountTransactionsAsync(
        string accountUid,
        string? dateFrom,
        string? continuationKey,
        CancellationToken ct = default);

    Task<StartAuthResponseDto> StartAuthorizationAsync(
        string aspspName,
        string aspspCountry,
        string redirectUrl,
        string state,
        DateTimeOffset validUntil,
        CancellationToken ct = default);

    Task<CreateSessionResponseDto> CreateSessionAsync(
        string code,
        CancellationToken ct = default);
}