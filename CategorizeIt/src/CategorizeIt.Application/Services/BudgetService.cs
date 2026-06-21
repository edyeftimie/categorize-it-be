using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Budgets;
using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgets;
    private readonly ITransactionRepository _transactions;

    public BudgetService(IBudgetRepository budgets, ITransactionRepository transactions)
    {
        _budgets = budgets;
        _transactions = transactions;
    }

    public async Task<IEnumerable<BudgetDto>> GetBudgetsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var budgets = await _budgets.GetByUserIdAsync(userId);
        var expensesByCategory = await _transactions.GetExpensesByCategoryAsync(userId, now.Month, now.Year);

        return budgets.Select(b =>
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
    }

    public async Task<Guid?> CreateBudgetAsync(Guid userId, CreateBudgetRequest request)
    {
        if (await _budgets.ExistsAsync(userId, request.CategoryId))
            return null;

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = request.CategoryId,
            MonthlyLimit = request.MonthlyLimit,
            Currency = request.Currency
        };

        await _budgets.CreateAsync(budget);
        return budget.Id;
    }

    public async Task<bool> UpdateBudgetAsync(Guid userId, Guid budgetId, UpdateBudgetRequest request)
    {
        var budget = await _budgets.GetByIdAsync(budgetId);
        if (budget == null || budget.UserId != userId)
            return false;

        budget.MonthlyLimit = request.MonthlyLimit;
        await _budgets.UpdateAsync(budget);
        return true;
    }

    public async Task<bool> DeleteBudgetAsync(Guid userId, Guid budgetId)
    {
        var budget = await _budgets.GetByIdAsync(budgetId);
        if (budget == null || budget.UserId != userId)
            return false;

        await _budgets.DeleteAsync(budget);
        return true;
    }
}