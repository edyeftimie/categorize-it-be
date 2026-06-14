using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Recommendations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationRepository _recommendations;

    public RecommendationsController(IRecommendationRepository recommendations)
    {
        _recommendations = recommendations;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendations()
    {
        var userId = GetUserId();
        var recommendations = await _recommendations.GetByUserIdAsync(userId);

        var result = recommendations.Select(r => new RecommendationDto
        {
            Id = r.Id,
            Type = r.Type,
            Title = r.Title,
            Description = r.Description,
            CategoryId = r.CategoryId,
            CategoryName = r.Category?.Name,
            CategoryColor = r.Category?.Color,
            Priority = r.Priority,
            IsRead = r.IsRead,
            IsDismissed = r.IsDismissed,
            CreatedAt = r.CreatedAt
        });

        return Ok(result);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        var rec = await _recommendations.GetByIdAsync(id);

        if (rec == null || rec.UserId != userId)
            return NotFound();

        rec.IsRead = true;
        await _recommendations.UpdateAsync(rec);
        return NoContent();
    }

    [HttpPatch("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id)
    {
        var userId = GetUserId();
        var rec = await _recommendations.GetByIdAsync(id);

        if (rec == null || rec.UserId != userId)
            return NotFound();

        rec.IsDismissed = true;
        await _recommendations.UpdateAsync(rec);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}