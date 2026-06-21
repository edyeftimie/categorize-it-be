using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? month, [FromQuery] int? year)
    {
        var dto = await _dashboard.GetDashboardAsync(GetUserId(), month, year);
        return Ok(dto);
    }

    [HttpGet("monthly-series/{categoryId}")]
    public async Task<IActionResult> GetMonthlySeries(Guid categoryId, [FromQuery] int months = 6)
    {
        var result = await _dashboard.GetMonthlySeriesAsync(GetUserId(), categoryId, months);
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}