
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Recommendations;
using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Services;

public class RecommendationService : IRecommendationService
{
    private const decimal TrendUpThreshold    = 0.30m;
    private const decimal TrendDownThreshold  = 0.20m;
    private const decimal SavingsTipThreshold = 0.90m;
    private const int     NoBudgetTopN        = 3;

    private readonly IRecommendationRepository _recommendations;
    private readonly ITransactionRepository    _transactions;
    private readonly IBudgetRepository         _budgets;

    public RecommendationService(
        IRecommendationRepository recommendations,
        ITransactionRepository    transactions,
        IBudgetRepository         budgets)
    {
        _recommendations = recommendations;
        _transactions    = transactions;
        _budgets         = budgets;
    }

    public async Task GenerateForUserAsync(Guid userId, (int Month, int Year)? period, CancellationToken ct = default)
    {
        var now   = DateTime.UtcNow;
        var month = period?.Month ?? now.Month;
        var year  = period?.Year  ?? now.Year;

        var prevMonth = month == 1 ? 12 : month - 1;
        var prevYear  = month == 1 ? year - 1 : year;

        var currentExpenses = await _transactions.GetExpensesByCategoryAsync(userId, month, year);
        var prevExpenses    = await _transactions.GetExpensesByCategoryAsync(userId, prevMonth, prevYear);
        var summary         = await _transactions.GetMonthlySummaryAsync(userId, month, year);
        var budgets         = (await _budgets.GetByUserIdAsync(userId)).ToList();

        var prevByCategory   = prevExpenses.ToDictionary(e => e.CategoryId, e => e.Total);
        var budgetByCategory = budgets.ToDictionary(b => b.CategoryId);

        var fresh = new List<Recommendation>();

        // Rule 1 — OVERSPEND (Priority 3)
        foreach (var expense in currentExpenses)
        {
            if (!budgetByCategory.TryGetValue(expense.CategoryId, out var budget)) continue;
            if (expense.Total <= budget.MonthlyLimit) continue;

            fresh.Add(Make(userId, "OVERSPEND", 3, expense.CategoryId,
                title:       $"{expense.CategoryName} is over budget",
                description: $"You've spent {expense.Total:F2} {budget.Currency} of your {budget.MonthlyLimit:F2} {budget.Currency} budget this month."));
        }

        // Rule 2 — TREND_UP (Priority 2)
        foreach (var expense in currentExpenses)
        {
            if (!prevByCategory.TryGetValue(expense.CategoryId, out var prev) || prev == 0) continue;
            var change = (expense.Total - prev) / prev;
            if (change < TrendUpThreshold) continue;

            fresh.Add(Make(userId, "TREND_UP", 2, expense.CategoryId,
                title:       $"{expense.CategoryName} spending is up {change * 100:F0}%",
                description: $"You spent {expense.Total:F2} this month vs {prev:F2} last month on {expense.CategoryName}."));
        }

        // Rule 3 — TREND_DOWN (Priority 1)
        foreach (var expense in currentExpenses)
        {
            if (!prevByCategory.TryGetValue(expense.CategoryId, out var prev) || prev == 0) continue;
            var change = (prev - expense.Total) / prev;
            if (change < TrendDownThreshold) continue;

            fresh.Add(Make(userId, "TREND_DOWN", 1, expense.CategoryId,
                title:       $"Great progress on {expense.CategoryName}!",
                description: $"Your {expense.CategoryName} spending dropped by {change * 100:F0}% compared to last month."));
        }

        // Rule 4 — SAVINGS_TIP (Priority 3)
        if (summary.Income > 0 && summary.Expenses / summary.Income >= SavingsTipThreshold)
        {
            var pct = summary.Expenses / summary.Income * 100;
            fresh.Add(Make(userId, "SAVINGS_TIP", 3, categoryId: null,
                title:       "You're spending most of your income",
                description: $"This month you spent {pct:F0}% of your income. Consider reviewing your expenses."));
        }

        // Rule 5 — NO_BUDGET (Priority 2)
        var noBudget = currentExpenses
            .Where(e => !budgetByCategory.ContainsKey(e.CategoryId))
            .OrderByDescending(e => e.Total)
            .Take(NoBudgetTopN);

        foreach (var expense in noBudget)
        {
            fresh.Add(Make(userId, "NO_BUDGET", 2, expense.CategoryId,
                title:       $"No budget for {expense.CategoryName}",
                description: $"You've spent {expense.Total:F2} on {expense.CategoryName} this month but haven't set a budget."));
        }

        // Reconcile — update-in-place to preserve IsRead / IsDismissed
        var stored      = await _recommendations.GetByUserIdAndMonthAsync(userId, month, year);
        var storedByKey = stored.ToDictionary(r => (r.Type, r.CategoryId));

        var toCreate = new List<Recommendation>();
        var toUpdate = new List<Recommendation>();

        foreach (var r in fresh)
        {
            var key = (r.Type, r.CategoryId);
            if (storedByKey.TryGetValue(key, out var existing))
            {
                existing.Title       = r.Title;
                existing.Description = r.Description;
                existing.Priority    = r.Priority;
                toUpdate.Add(existing);
                storedByKey.Remove(key);
            }
            else
            {
                toCreate.Add(r);
            }
        }

        var toDelete = storedByKey.Values.ToList();

        if (toCreate.Count > 0) await _recommendations.CreateRangeAsync(toCreate);
        if (toUpdate.Count > 0) await _recommendations.UpdateRangeAsync(toUpdate);
        if (toDelete.Count > 0) await _recommendations.DeleteRangeAsync(toDelete);
    }

    public async Task<IEnumerable<RecommendationDto>> GetCurrentMonthAsync(
        Guid userId, bool includeRead = false, bool includeDismissed = false)
    {
        var now    = DateTime.UtcNow;
        var stored = await _recommendations.GetByUserIdAndMonthAsync(userId, now.Month, now.Year);

        return stored
            .Where(r => (includeRead    || !r.IsRead)
                     && (includeDismissed || !r.IsDismissed))
            .OrderByDescending(r => r.Priority)
            .Select(ToDto);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var now    = DateTime.UtcNow;
        var stored = await _recommendations.GetByUserIdAndMonthAsync(userId, now.Month, now.Year);
        return stored.Count(r => !r.IsRead && !r.IsDismissed);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid recommendationId)
    {
        var r = await _recommendations.GetByIdAsync(recommendationId);
        if (r == null || r.UserId != userId) return false;
        r.IsRead = true;
        await _recommendations.UpdateAsync(r);
        return true;
    }

    public async Task<bool> DismissAsync(Guid userId, Guid recommendationId)
    {
        var r = await _recommendations.GetByIdAsync(recommendationId);
        if (r == null || r.UserId != userId) return false;
        r.IsDismissed = true;
        await _recommendations.UpdateAsync(r);
        return true;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Recommendation Make(
        Guid userId, string type, int priority, Guid? categoryId,
        string title, string description) => new()
    {
        Id          = Guid.NewGuid(),
        UserId      = userId,
        Type        = type,
        Title       = title,
        Description = description,
        CategoryId  = categoryId,
        Priority    = priority,
        IsRead      = false,
        IsDismissed = false,
        CreatedAt   = DateTime.UtcNow
    };

    private static RecommendationDto ToDto(Recommendation r) => new()
    {
        Id            = r.Id,
        Type          = r.Type,
        Title         = r.Title,
        Description   = r.Description,
        CategoryId    = r.CategoryId,
        CategoryName  = r.Category?.Name,
        CategoryColor = r.Category?.Color,
        Priority      = r.Priority,
        IsRead        = r.IsRead,
        IsDismissed   = r.IsDismissed,
        CreatedAt     = r.CreatedAt
    };
}