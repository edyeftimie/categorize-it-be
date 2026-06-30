using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/bank-connections")]
[Authorize]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionService _service;

    public BankConnectionsController(IBankConnectionService service)
    {
        _service = service;
    }

    [HttpPost("auth")]
    public async Task<IActionResult> InitiateAuth([FromBody] InitiateAuthRequest request, CancellationToken ct)
    {
        var result = await _service.InitiateAuthAsync(request.AspspName, request.AspspCountry, ct);
        return Ok(new { url = result.Url, state = result.State });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] CallbackRequest request, CancellationToken ct)
    {
        var result = await _service.HandleCallbackAsync(GetUserId(), request.Code, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        var result = await _service.GetConnectionsAsync(GetUserId());
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _service.DeleteConnectionAsync(GetUserId(), id);
        return ok ? NoContent() : NotFound();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public record InitiateAuthRequest(string AspspName, string AspspCountry);
public record CallbackRequest(string Code);