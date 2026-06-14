using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Dashboard;
using CategorizeIt.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ITransactionRepository _transactions;

    public DashboardController(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var m = month ?? now.Month;
        var y = year ?? now.Year;

        var balance = await _transactions.GetAllTimeBalanceAsync(userId);
        var (income, expenses) = await _transactions.GetMonthlySummaryAsync(userId, m, y);
        var byCategory = await _transactions.GetExpensesByCategoryAsync(userId, m, y);

        var topCategories = byCategory.Select(c => new CategorySpendingDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            CategoryColor = c.CategoryColor,
            CategoryIcon = c.CategoryIcon,
            Amount = c.Total,
            Percentage = expenses > 0 ? Math.Round(c.Total / expenses * 100, 1) : 0
        }).ToList();

        var dto = new DashboardDto
        {
            TotalBalance = balance,
            TotalIncome = income,
            TotalExpenses = expenses,
            TopCategories = topCategories,
            NeedWantSplit = new NeedWantSplitDto()
        };

        return Ok(dto);
    }

    [HttpGet("monthly-series/{categoryId}")]
    public async Task<IActionResult> GetMonthlySeries(Guid categoryId, [FromQuery] int months = 6)
    {
        var userId = GetUserId();
        var series = await _transactions.GetMonthlySeriesAsync(userId, categoryId, months);

        var result = series.Select(s => new MonthlyAmountDto
        {
            Month = s.Month,
            Year = s.Year,
            Total = s.Total
        });

        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}