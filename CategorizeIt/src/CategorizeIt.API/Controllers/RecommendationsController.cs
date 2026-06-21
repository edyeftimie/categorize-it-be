using CategorizeIt.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _service;

    public RecommendationsController(IRecommendationService service)
        => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetCurrentMonth(
        [FromQuery] Guid userId,
        [FromQuery] bool includeRead      = false,
        [FromQuery] bool includeDismissed = false,
        CancellationToken ct = default)
    {
        var result = await _service.GetCurrentMonthAsync(userId, includeRead, includeDismissed);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        [FromQuery] Guid userId,
        CancellationToken ct = default)
    {
        var count = await _service.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromQuery] Guid userId,
        CancellationToken ct = default)
    {
        await _service.GenerateForUserAsync(userId, null, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, [FromQuery] Guid userId, CancellationToken ct = default)
    {
        var ok = await _service.MarkAsReadAsync(userId, id);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, [FromQuery] Guid userId, CancellationToken ct = default)
    {
        var ok = await _service.DismissAsync(userId, id);
        return ok ? NoContent() : NotFound();
    }
}