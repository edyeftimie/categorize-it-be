using CategorizeIt.Domain.Entities;
using CategorizeIt.Application.Models.Transactions;

namespace CategorizeIt.Application.Interfaces;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetByUserIdAsync(Guid userId, TransactionFilters filters);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task CreateAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task<List<(Guid CategoryId, string CategoryName, string? CategoryColor, string? CategoryIcon, decimal Total)>> GetExpensesByCategoryAsync(Guid userId, int month, int year);
    Task<(decimal Income, decimal Expenses)> GetMonthlySummaryAsync(Guid userId, int month, int year);
    Task<decimal> GetAllTimeBalanceAsync(Guid userId);
    Task<List<(int Month, int Year, decimal Total)>> GetMonthlySeriesAsync(Guid userId, Guid categoryId, int months);
}