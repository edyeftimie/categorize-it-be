using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.BankConnections;
using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Services;

public class BankConnectionService : IBankConnectionService
{
    private readonly IEnableBankingClient _enableBanking;
    private readonly IBankConnectionRepository _connections;

    public BankConnectionService(IEnableBankingClient enableBanking, IBankConnectionRepository connections)
    {
        _enableBanking = enableBanking;
        _connections = connections;
    }

    public async Task<InitiateAuthResult> InitiateAuthAsync(string aspspName, string aspspCountry, CancellationToken ct)
    {
        var state = Guid.NewGuid().ToString("N");
        var validUntil = DateTimeOffset.UtcNow.AddDays(90);

        var result = await _enableBanking.StartAuthorizationAsync(
            aspspName, aspspCountry,
            "http://localhost:3000/bank-callback",
            state, validUntil, ct);

        return new InitiateAuthResult(result.Url, state);
    }

    public async Task<BankConnectionDto> HandleCallbackAsync(Guid userId, string code, CancellationToken ct)
    {
        var session = await _enableBanking.CreateSessionAsync(code, ct);

        var connectionId = Guid.NewGuid();
        var connection = new BankConnection
        {
            Id = connectionId,
            UserId = userId,
            SessionId = session.SessionId,
            AspspName = session.Aspsp.Name,
            AspspCountry = session.Aspsp.Country,
            PsuType = session.PsuType,
            ValidUntil = DateTime.SpecifyKind(session.Access.ValidUntil, DateTimeKind.Utc),
            Status = "AUTHORIZED",
            CreatedAt = DateTime.UtcNow,
            BankAccounts = session.Accounts.Select(a => new BankAccount
            {
                Id = Guid.NewGuid(),
                BankConnectionId = connectionId,
                Uid = a.Uid,
                IdentificationHash = a.IdentificationHash,
                Name = a.Name,
                Currency = a.Currency,
                CashAccountType = a.CashAccountType,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        await _connections.CreateAsync(connection);
        return ToDto(connection);
    }

    public async Task<IEnumerable<BankConnectionDto>> GetConnectionsAsync(Guid userId)
    {
        var connections = await _connections.GetByUserIdAsync(userId);
        return connections.Select(ToDto);
    }

    public async Task<bool> DeleteConnectionAsync(Guid id)
    {
        var connection = await _connections.GetByIdAsync(id);
        if (connection == null)
            return false;

        await _connections.DeleteAsync(connection);
        return true;
    }

    private static BankConnectionDto ToDto(BankConnection c) => new()
    {
        Id = c.Id,
        AspspName = c.AspspName,
        AspspCountry = c.AspspCountry,
        Status = c.Status,
        ValidUntil = c.ValidUntil,
        CreatedAt = c.CreatedAt,
        Accounts = c.BankAccounts.Select(a => new BankAccountDto
        {
            Id = a.Id,
            Uid = a.Uid,
            Iban = a.Iban,
            Name = a.Name,
            Currency = a.Currency,
            CashAccountType = a.CashAccountType,
            LastSyncedAt = a.LastSyncedAt
        }).ToList()
    };
}