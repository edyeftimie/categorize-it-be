using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.BankConnections;
using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Services;

public class BankConnectionService : IBankConnectionService
{
    private readonly IEnableBankingClient _enableBanking;
    private readonly IBankConnectionRepository _connections;

    private const string RedirectUrl = "https://edyeftimie.github.io/categoriseit-callback/";

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
            aspspName, aspspCountry, RedirectUrl, state, validUntil, ct);
        return new InitiateAuthResult(result.Url, state);
    }

    public async Task<BankConnectionDto> HandleCallbackAsync(Guid userId, string code, CancellationToken ct)
    {
        var session = await _enableBanking.CreateSessionAsync(code, ct);

        // Match existing accounts by IdentificationHash (STABLE across reconnects).
        // Uid changes each session, so matching on Uid wrongly triggers CREATE and
        // collides on the unique IdentificationHash constraint.
        var incomingHashes = session.Accounts
            .Select(a => a.IdentificationHash)
            .Where(h => h != null)
            .ToList();

        var existingAccounts = await _connections
            .GetAccountsByIdentificationHashesForUserAsync(userId, incomingHashes);

        BankConnection connection;

        if (existingAccounts.Count > 0)
        {
            // REACTIVATE existing connection in place
            connection = existingAccounts[0].BankConnection;

            connection.SessionId    = session.SessionId;
            connection.AspspName    = session.Aspsp.Name;
            connection.AspspCountry = session.Aspsp.Country;
            connection.PsuType      = session.PsuType;
            connection.ValidUntil   = DateTime.SpecifyKind(session.Access.ValidUntil, DateTimeKind.Utc);
            connection.Status       = "AUTHORIZED";

            var existingByHash = existingAccounts
                .Where(a => a.IdentificationHash != null)
                .ToDictionary(a => a.IdentificationHash!);

            foreach (var incoming in session.Accounts)
            {
                if (incoming.IdentificationHash != null &&
                    existingByHash.TryGetValue(incoming.IdentificationHash, out var existing))
                {
                    existing.Uid             = incoming.Uid;
                    existing.Name            = incoming.Name;
                    existing.Currency        = incoming.Currency;
                    existing.CashAccountType = incoming.CashAccountType;
                }
                else
                {
                    connection.BankAccounts.Add(new BankAccount
                    {
                        Id                 = Guid.NewGuid(),
                        BankConnectionId   = connection.Id,
                        Uid                = incoming.Uid,
                        IdentificationHash = incoming.IdentificationHash,
                        Name               = incoming.Name,
                        Currency           = incoming.Currency,
                        CashAccountType    = incoming.CashAccountType,
                        CreatedAt          = DateTime.UtcNow
                    });
                }
            }

            await _connections.UpdateAsync(connection);
        }
        else
        {
            // CREATE path — brand-new bank
            var connectionId = Guid.NewGuid();
            connection = new BankConnection
            {
                Id           = connectionId,
                UserId       = userId,
                SessionId    = session.SessionId,
                AspspName    = session.Aspsp.Name,
                AspspCountry = session.Aspsp.Country,
                PsuType      = session.PsuType,
                ValidUntil   = DateTime.SpecifyKind(session.Access.ValidUntil, DateTimeKind.Utc),
                Status       = "AUTHORIZED",
                CreatedAt    = DateTime.UtcNow,
                BankAccounts = session.Accounts.Select(a => new BankAccount
                {
                    Id                 = Guid.NewGuid(),
                    BankConnectionId   = connectionId,
                    Uid                = a.Uid,
                    IdentificationHash = a.IdentificationHash,
                    Name               = a.Name,
                    Currency           = a.Currency,
                    CashAccountType    = a.CashAccountType,
                    CreatedAt          = DateTime.UtcNow
                }).ToList()
            };

            await _connections.CreateAsync(connection);
        }

        return ToDto(connection);
    }

    public async Task<IEnumerable<BankConnectionDto>> GetConnectionsAsync(Guid userId)
    {
        var connections = await _connections.GetByUserIdAsync(userId);
        return connections.Select(ToDto);
    }

    // Ownership check added — only the owner can disconnect.
    public async Task<bool> DeleteConnectionAsync(Guid userId, Guid id)
    {
        var connection = await _connections.GetByIdAsync(id);
        if (connection == null || connection.UserId != userId)
            return false;

        await _connections.DeleteAsync(connection); // soft-delete (Status = "DISCONNECTED")
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