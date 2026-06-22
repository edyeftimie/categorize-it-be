using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Dashboard;
using CategorizeIt.Domain.Enums;

namespace CategorizeIt.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITransactionRepository _transactions;
    private readonly IBudgetRepository _budgets;
    private readonly IMccCategoriser _mccCategoriser = new MccCategoriser();

    public DashboardService(ITransactionRepository transactions, IBudgetRepository budgets, IMccCategoriser mccCategoriser)
    {
        _transactions = transactions;
        _budgets = budgets;
        _mccCategoriser = mccCategoriser;
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId, int? month, int? year)
    {
        var now = DateTime.UtcNow;
        var m = month ?? now.Month;
        var y = year ?? now.Year;

        var balance = await _transactions.GetAllTimeBalanceAsync(userId);
        var (income, expenses) = await _transactions.GetMonthlySummaryAsync(userId, m, y);
        var byCategory = await _transactions.GetExpensesByCategoryAsync(userId, m, y);
        var budgets = await _budgets.GetByUserIdAsync(userId);

        var budgetMap = budgets.ToDictionary(b => b.CategoryId, b => b.MonthlyLimit);

        var topCategories = byCategory.Select(c => new CategorySpendingDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            CategoryColor = c.CategoryColor,
            CategoryIcon = c.CategoryIcon,
            Amount = c.Total,
            Percentage = expenses > 0 ? Math.Round(c.Total / expenses * 100, 1) : 0,
            BudgetLimit = budgetMap.TryGetValue(c.CategoryId, out var limit) ? limit : null
        }).ToList();

        // Compute Need/Want/Savings split from each transaction's MCC
        var monthExpenses = await _transactions.GetExpensesForMonthAsync(userId, m, y);
        var split = new NeedWantSplitDto();
        foreach (var t in monthExpenses)
        {
            var (_, classification) = _mccCategoriser.Classify(t.MerchantCategoryCode);
            switch (classification)
            {
                case NeedWantSavings.Need:
                    split.NeedAmount += t.Amount;
                    break;
                case NeedWantSavings.Want:
                    split.WantAmount += t.Amount;
                    break;
                case NeedWantSavings.Savings:
                    split.SavingsAmount += t.Amount;
                    break;
                default:
                    split.UncategorisedAmount += t.Amount;
                    break;
            }
        }

        return new DashboardDto
        {
            TotalBalance = balance,
            TotalIncome = income,
            TotalExpenses = expenses,
            TopCategories = topCategories,
            NeedWantSplit = split
        };
    }

    public async Task<IEnumerable<MonthlyAmountDto>> GetMonthlySeriesAsync(Guid userId, Guid categoryId, int months)
    {
        var series = await _transactions.GetMonthlySeriesAsync(userId, categoryId, months);
        return series.Select(s => new MonthlyAmountDto
        {
            Month = s.Month,
            Year = s.Year,
            Total = s.Total
        });
    }
}