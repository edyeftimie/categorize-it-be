using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/bank-connections")]
[Authorize]
public class BankConnectionsController : ControllerBase
{
    private readonly IEnableBankingClient _enableBanking;
    private readonly IBankConnectionRepository _connections;

    public BankConnectionsController(
        IEnableBankingClient enableBanking,
        IBankConnectionRepository connections)
    {
        _enableBanking = enableBanking;
        _connections   = connections;
    }

    [HttpPost("auth")]
    public async Task<IActionResult> InitiateAuth(
        [FromBody] InitiateAuthRequest request,
        CancellationToken ct)
    {
        var state      = Guid.NewGuid().ToString("N");
        var validUntil = DateTimeOffset.UtcNow.AddDays(90);

        var result = await _enableBanking.StartAuthorizationAsync(
            request.AspspName,
            request.AspspCountry,
            "https://edyeftimie.github.io/categoriseit-callback/",
            state,
            validUntil,
            ct);

        return Ok(new { url = result.Url, state });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromBody] CallbackRequest request,
        CancellationToken ct)
    {
        var userId  = GetUserId();
        var session = await _enableBanking.CreateSessionAsync(request.Code, ct);

        // Match existing accounts by IdentificationHash (STABLE across reconnects).
        // Uid changes each session, so matching on Uid wrongly triggers CREATE and
        // collides on the unique IdentificationHash constraint.
        var incomingHashes   = session.Accounts
            .Select(a => a.IdentificationHash)
            .Where(h => h != null)
            .ToList();
        var existingAccounts = await _connections
            .GetAccountsByIdentificationHashesForUserAsync(userId, incomingHashes);

        if (existingAccounts.Count > 0)
        {
            // REACTIVATE existing connection in place
            var existingConnection = existingAccounts[0].BankConnection;

            existingConnection.SessionId    = session.SessionId;
            existingConnection.AspspName    = session.Aspsp.Name;
            existingConnection.AspspCountry = session.Aspsp.Country;
            existingConnection.PsuType      = session.PsuType;
            existingConnection.ValidUntil   = DateTime.SpecifyKind(session.Access.ValidUntil, DateTimeKind.Utc);
            existingConnection.Status       = "AUTHORIZED";

            var existingByHash = existingAccounts
                .Where(a => a.IdentificationHash != null)
                .ToDictionary(a => a.IdentificationHash!);

            foreach (var incoming in session.Accounts)
            {
                if (incoming.IdentificationHash != null &&
                    existingByHash.TryGetValue(incoming.IdentificationHash, out var existing))
                {
                    // Refresh metadata; Id and BankConnectionId stay unchanged.
                    // Update the Uid too, since it changes per session.
                    existing.Uid             = incoming.Uid;
                    existing.Name            = incoming.Name;
                    existing.Currency        = incoming.Currency;
                    existing.CashAccountType = incoming.CashAccountType;
                }
                else
                {
                    // New account added since last connection
                    existingConnection.BankAccounts.Add(new BankAccount
                    {
                        Id                 = Guid.NewGuid(),
                        BankConnectionId   = existingConnection.Id,
                        Uid                = incoming.Uid,
                        IdentificationHash = incoming.IdentificationHash,
                        Name               = incoming.Name,
                        Currency           = incoming.Currency,
                        CashAccountType    = incoming.CashAccountType,
                        CreatedAt          = DateTime.UtcNow
                    });
                }
            }

            await _connections.UpdateAsync(existingConnection);

            return Ok(new
            {
                id           = existingConnection.Id,
                aspspName    = existingConnection.AspspName,
                aspspCountry = existingConnection.AspspCountry,
                status       = existingConnection.Status,
                validUntil   = existingConnection.ValidUntil,
                createdAt    = existingConnection.CreatedAt,
                accounts     = existingConnection.BankAccounts.Select(a => new
                {
                    id              = a.Id,
                    uid             = a.Uid,
                    name            = a.Name,
                    currency        = a.Currency,
                    cashAccountType = a.CashAccountType
                })
            });
        }

        // CREATE path — no existing accounts matched, brand-new bank
        var connectionId = Guid.NewGuid();

        var connection = new BankConnection
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

        return Ok(new
        {
            id           = connection.Id,
            aspspName    = connection.AspspName,
            aspspCountry = connection.AspspCountry,
            status       = connection.Status,
            validUntil   = connection.ValidUntil,
            createdAt    = connection.CreatedAt,
            accounts     = connection.BankAccounts.Select(a => new
            {
                id              = a.Id,
                uid             = a.Uid,
                name            = a.Name,
                currency        = a.Currency,
                cashAccountType = a.CashAccountType
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        var userId      = GetUserId();
        var connections = await _connections.GetByUserIdAsync(userId);

        return Ok(connections.Select(c => new
        {
            id           = c.Id,
            aspspName    = c.AspspName,
            aspspCountry = c.AspspCountry,
            status       = c.Status,
            validUntil   = c.ValidUntil,
            createdAt    = c.CreatedAt,
            accounts     = c.BankAccounts.Select(a => new
            {
                id              = a.Id,
                uid             = a.Uid,
                iban            = a.Iban,
                name            = a.Name,
                currency        = a.Currency,
                cashAccountType = a.CashAccountType,
                lastSyncedAt    = a.LastSyncedAt
            })
        }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId     = GetUserId();
        var connection = await _connections.GetByIdAsync(id);

        if (connection == null || connection.UserId != userId)
            return NotFound();

        await _connections.DeleteAsync(connection); // soft-deletes via Status = "DISCONNECTED"
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public record InitiateAuthRequest(string AspspName, string AspspCountry);
public record CallbackRequest(string Code);