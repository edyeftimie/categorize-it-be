using CategorizeIt.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/bank-connections")]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionService _bankConnectionService;

    public BankConnectionsController(IBankConnectionService bankConnectionService)
    {
        _bankConnectionService = bankConnectionService;
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
        var connection = await _bankConnectionService.HandleCallbackAsync(request.UserId, request.Code, ct);
        return Ok(connection);
    }

    [HttpGet]
    public async Task<IActionResult> GetConnections([FromQuery] Guid userId, CancellationToken ct)
    {
        var connections = await _bankConnectionService.GetConnectionsAsync(userId);
        return Ok(connections);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var found = await _bankConnectionService.DeleteConnectionAsync(id);
        return found ? NoContent() : NotFound();
    }
}

public record InitiateAuthRequest(Guid UserId, string AspspName, string AspspCountry);
public record CallbackRequest(Guid UserId, string Code);