using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CategorizeIt.Application.Interfaces;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/bank-connections")]
[Authorize]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionService _bankConnectionService;
    private readonly IEnableBankingClient _enableBankingClient;

    public BankConnectionsController(IBankConnectionService bankConnectionService, IEnableBankingClient enableBankingClient)
    {
        _bankConnectionService = bankConnectionService;
        _enableBankingClient = enableBankingClient;
    }

    [HttpPost("auth")]
    public async Task<IActionResult> InitiateAuth([FromBody] InitiateAuthRequest request, CancellationToken ct)
    {
        var result = await _bankConnectionService.InitiateAuthAsync(request.AspspName, request.AspspCountry, ct);
        return Ok(new { url = result.Url, state = result.State });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] CallbackRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var connection = await _bankConnectionService.HandleCallbackAsync(userId, request.Code, ct);
        return Ok(connection);
    }

    [HttpGet]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        var userId = GetUserId();
        var connections = await _bankConnectionService.GetConnectionsAsync(userId);
        return Ok(connections);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var found = await _bankConnectionService.DeleteConnectionAsync(id);
        return found ? NoContent() : NotFound();
    }
    
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public record InitiateAuthRequest(string AspspName, string AspspCountry);
public record CallbackRequest(string Code);