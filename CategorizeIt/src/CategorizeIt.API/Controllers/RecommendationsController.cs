using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendations()
    {
        var result = await _recommendationService.GetRecommendationsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var found = await _recommendationService.MarkAsReadAsync(GetUserId(), id);
        return found ? NoContent() : NotFound();
    }

    [HttpPatch("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id)
    {
        var found = await _recommendationService.DismissAsync(GetUserId(), id);
        return found ? NoContent() : NotFound();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}