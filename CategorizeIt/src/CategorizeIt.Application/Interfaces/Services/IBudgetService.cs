using CategorizeIt.Application.Models.Budgets;

namespace CategorizeIt.Application.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetDto>> GetBudgetsAsync(Guid userId);
    Task<Guid?> CreateBudgetAsync(Guid userId, CreateBudgetRequest request);
    Task<bool> UpdateBudgetAsync(Guid userId, Guid budgetId, UpdateBudgetRequest request);
    Task<bool> DeleteBudgetAsync(Guid userId, Guid budgetId);
}