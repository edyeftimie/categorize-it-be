using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Budgets;
using CategorizeIt.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetRepository _budgets;
    private readonly ITransactionRepository _transactions;

    public BudgetsController(IBudgetRepository budgets, ITransactionRepository transactions)
    {
        _budgets = budgets;
        _transactions = transactions;
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgets()
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var budgets = await _budgets.GetByUserIdAsync(userId);
        var expensesByCategory = await _transactions.GetExpensesByCategoryAsync(userId, now.Month, now.Year);

        var result = budgets.Select(b =>
        {
            var spent = expensesByCategory.FirstOrDefault(e => e.CategoryId == b.CategoryId).Total;
            return new BudgetDto
            {
                Id = b.Id,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name,
                CategoryIcon = b.Category.Icon,
                CategoryColor = b.Category.Color,
                MonthlyLimit = b.MonthlyLimit,
                AmountSpent = spent,
                Currency = b.Currency
            };
        });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetRequest request)
    {
        var userId = GetUserId();

        if (await _budgets.ExistsAsync(userId, request.CategoryId))
            return Conflict("A budget for this category already exists.");

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = request.CategoryId,
            MonthlyLimit = request.MonthlyLimit,
            Currency = request.Currency
        };

        await _budgets.CreateAsync(budget);
        return Ok(new { budget.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var userId = GetUserId();
        var budget = await _budgets.GetByIdAsync(id);

        if (budget == null || budget.UserId != userId)
            return NotFound();

        budget.MonthlyLimit = request.MonthlyLimit;
        await _budgets.UpdateAsync(budget);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = GetUserId();
        var budget = await _budgets.GetByIdAsync(id);

        if (budget == null || budget.UserId != userId)
            return NotFound();

        await _budgets.DeleteAsync(budget);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}