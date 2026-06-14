using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.BankConnections;
using CategorizeIt.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/bank-connections")]
[Authorize]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionRepository _connections;

    public BankConnectionsController(IBankConnectionRepository connections)
    {
        _connections = connections;
    }

    [HttpGet]
    public async Task<IActionResult> GetConnections()
    {
        var userId = GetUserId();
        var connections = await _connections.GetByUserIdAsync(userId);

        var result = connections.Select(c => new BankConnectionDto
        {
            Id = c.Id,
            AspspName = c.AspspName,
            Status = c.Status,
            ValidUntil = c.ValidUntil,
            CreatedAt = c.CreatedAt,
            Accounts = c.BankAccounts.Select(a => new BankAccountDto
            {
                Id = a.Id,
                Iban = a.Iban,
                Name = a.Name,
                Currency = a.Currency,
                CashAccountType = a.CashAccountType,
                LastSyncedAt = a.LastSyncedAt
            }).ToList()
        });

        return Ok(result);
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateAuth([FromBody] InitiateAuthRequest request)
    {
        var userId = GetUserId();

        // Placeholder — wire Enable Banking SDK here in Phase 7
        var sessionId = Guid.NewGuid().ToString();
        var authUrl = $"https://enablebanking.com/auth?session={sessionId}&aspsp={request.AspspName}";

        var connection = new BankConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            AspspName = request.AspspName,
            AspspCountry = request.AspspCountry,
            ValidUntil = DateTime.UtcNow.AddDays(90),
            Status = "Pending"
        };

        await _connections.CreateAsync(connection);

        return Ok(new InitiateAuthResponse { AuthUrl = authUrl, SessionId = sessionId });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Disconnect(Guid id)
    {
        var userId = GetUserId();
        var connection = await _connections.GetByIdAsync(id);

        if (connection == null || connection.UserId != userId)
            return NotFound();

        await _connections.DeleteAsync(connection);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}