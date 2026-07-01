using Xunit;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Services;
using CategorizeIt.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CategorizeIt.Application.Tests;

public class RecommendationServiceTests
{
    private readonly Mock<IRecommendationRepository> _recoRepo = new();
    private readonly Mock<ITransactionRepository>    _txRepo   = new();
    private readonly Mock<IBudgetRepository>         _budgetRepo = new();

    private RecommendationService CreateSut() =>
        new(_recoRepo.Object, _txRepo.Object, _budgetRepo.Object);

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CatId  = Guid.NewGuid();

    private static (Guid CategoryId, string CategoryName, string? CategoryColor, string? CategoryIcon, decimal Total)
        Expense(Guid catId, string name, decimal total) => (catId, name, null, null, total);

    private void SetupEmpty()
    {
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)>());
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((0m, 0m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>()))
                   .ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());
    }

    // ── GenerateForUserAsync — empty inputs ───────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_NoExpensesNoBudgets_CreatesNoRecommendations()
    {
        SetupEmpty();
        var sut = CreateSut();

        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Recommendation>>()), Times.Never);
    }

    // ── GenerateForUserAsync — OVERSPEND ──────────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_SpendingExceedsBudget_CreatesOverspendRecommendation()
    {
        var budget = new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = CatId, MonthlyLimit = 100m, Currency = "RON" };

        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)>
               {
                   Expense(CatId, "Food & Dining", 150m)
               });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((200m, 150m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget> { budget });
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "OVERSPEND" && rec.CategoryId == CatId))), Times.Once);
    }

    [Fact]
    public async Task GenerateForUserAsync_SpendingUnderBudget_NoOverspendRecommendation()
    {
        var budget = new Budget { Id = Guid.NewGuid(), UserId = UserId, CategoryId = CatId, MonthlyLimit = 200m, Currency = "RON" };

        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Food", 100m) });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((300m, 100m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget> { budget });
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "OVERSPEND"))), Times.Never);
    }

    // ── GenerateForUserAsync — TREND_UP ───────────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_SpendingUp30PctOrMore_CreatesTrendUpRecommendation()
    {
        // current month call returns current expenses; prev month call returns previous expenses
        var callCount = 0;
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(() =>
               {
                   callCount++;
                   return callCount == 1
                       ? new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Shopping", 130m) }
                       : new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Shopping", 100m) };
               });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((300m, 130m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "TREND_UP" && rec.CategoryId == CatId))), Times.Once);
    }

    [Fact]
    public async Task GenerateForUserAsync_SpendingUpBelow30Pct_NoTrendUpRecommendation()
    {
        var callCount = 0;
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(() =>
               {
                   callCount++;
                   return callCount == 1
                       ? new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Shopping", 120m) }
                       : new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Shopping", 100m) };
               });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((300m, 120m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "TREND_UP"))), Times.Never);
    }

    // ── GenerateForUserAsync — TREND_DOWN ─────────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_SpendingDown20PctOrMore_CreatesTrendDownRecommendation()
    {
        var callCount = 0;
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(() =>
               {
                   callCount++;
                   return callCount == 1
                       ? new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Shopping", 80m) }
                       : new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Shopping", 100m) };
               });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((300m, 80m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "TREND_DOWN" && rec.CategoryId == CatId))), Times.Once);
    }

    // ── GenerateForUserAsync — SAVINGS_TIP ───────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_ExpensesAt90PctOfIncome_CreatesSavingsTipRecommendation()
    {
        SetupEmpty();
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((1000m, 900m));

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "SAVINGS_TIP"))), Times.Once);
    }

    [Fact]
    public async Task GenerateForUserAsync_ExpensesBelow90PctOfIncome_NoSavingsTip()
    {
        SetupEmpty();
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((1000m, 800m));

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "SAVINGS_TIP"))), Times.Never);
    }

    [Fact]
    public async Task GenerateForUserAsync_ZeroIncome_NoSavingsTip()
    {
        SetupEmpty();
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((0m, 100m));

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "SAVINGS_TIP"))), Times.Never);
    }

    // ── GenerateForUserAsync — NO_BUDGET ─────────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_CategoryWithSpendButNoBudget_CreatesNoBudgetRecommendation()
    {
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Entertainment", 50m) });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((500m, 50m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Any(rec => rec.Type == "NO_BUDGET" && rec.CategoryId == CatId))), Times.Once);
    }

    [Fact]
    public async Task GenerateForUserAsync_NoBudgetLimitedToTop3Categories()
    {
        var cats = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var expenses = cats.Select((id, i) =>
            Expense(id, $"Cat{i}", (5 - i) * 10m)).ToList();

        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(expenses);
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((1000m, 100m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation>());

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<Recommendation>>(
            list => list.Count(rec => rec.Type == "NO_BUDGET") == 3)), Times.Once);
    }

    // ── GenerateForUserAsync — reconcile (update existing) ───────────────────

    [Fact]
    public async Task GenerateForUserAsync_ExistingRecommendationForSameTypeAndCategory_UpdatesInsteadOfCreate()
    {
        var existingReco = new Recommendation
        {
            Id = Guid.NewGuid(), UserId = UserId, Type = "NO_BUDGET",
            CategoryId = CatId, Title = "old", Description = "old", Priority = 1
        };

        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)> { Expense(CatId, "Ent", 50m) });
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync((500m, 50m));
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<Recommendation> { existingReco });

        var sut = CreateSut();
        await sut.GenerateForUserAsync(UserId, null);

        _recoRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Recommendation>>()), Times.Once);
        _recoRepo.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Recommendation>>()), Times.Never);
    }

    // ── GenerateForUserAsync — explicit period ────────────────────────────────

    [Fact]
    public async Task GenerateForUserAsync_WithExplicitPeriod_UsesSuppliedMonthYear()
    {
        SetupEmpty();
        var sut = CreateSut();

        await sut.GenerateForUserAsync(UserId, (1, 2025));

        // Verify prev-month is Dec 2024 (month==1 → prevMonth=12, prevYear=2024)
        _txRepo.Verify(r => r.GetExpensesByCategoryAsync(UserId, 12, 2024), Times.Once);
        _txRepo.Verify(r => r.GetExpensesByCategoryAsync(UserId, 1, 2025), Times.Once);
    }

    // ── GetCurrentMonthAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentMonthAsync_FiltersReadAndDismissedByDefault()
    {
        var recos = new List<Recommendation>
        {
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = "T", Title = "a", Description = "", Priority = 1, IsRead = false, IsDismissed = false },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = "T", Title = "b", Description = "", Priority = 1, IsRead = true,  IsDismissed = false },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = "T", Title = "c", Description = "", Priority = 1, IsRead = false, IsDismissed = true  },
        };
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(recos);

        var sut = CreateSut();
        var result = await sut.GetCurrentMonthAsync(UserId);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("a");
    }

    [Fact]
    public async Task GetCurrentMonthAsync_IncludeReadTrue_ReturnsReadOnes()
    {
        var recos = new List<Recommendation>
        {
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = "T", Title = "a", Description = "", Priority = 1, IsRead = false, IsDismissed = false },
            new() { Id = Guid.NewGuid(), UserId = UserId, Type = "T", Title = "b", Description = "", Priority = 1, IsRead = true,  IsDismissed = false },
        };
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(recos);

        var sut = CreateSut();
        var result = await sut.GetCurrentMonthAsync(UserId, includeRead: true);

        result.Should().HaveCount(2);
    }

    // ── GetUnreadCountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnreadNotDismissed()
    {
        var recos = new List<Recommendation>
        {
            new() { IsRead = false, IsDismissed = false },
            new() { IsRead = true,  IsDismissed = false },
            new() { IsRead = false, IsDismissed = true  },
        };
        _recoRepo.Setup(r => r.GetByUserIdAndMonthAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(recos);

        var count = await CreateSut().GetUnreadCountAsync(UserId);

        count.Should().Be(1);
    }

    // ── MarkAsReadAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAsReadAsync_ValidOwner_SetsIsReadAndReturnsTrue()
    {
        var recoId = Guid.NewGuid();
        var reco = new Recommendation { Id = recoId, UserId = UserId, IsRead = false };
        _recoRepo.Setup(r => r.GetByIdAsync(recoId)).ReturnsAsync(reco);

        var result = await CreateSut().MarkAsReadAsync(UserId, recoId);

        result.Should().BeTrue();
        reco.IsRead.Should().BeTrue();
        _recoRepo.Verify(r => r.UpdateAsync(reco), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_NotFound_ReturnsFalse()
    {
        _recoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Recommendation?)null);

        var result = await CreateSut().MarkAsReadAsync(UserId, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_WrongUser_ReturnsFalse()
    {
        var recoId = Guid.NewGuid();
        var reco = new Recommendation { Id = recoId, UserId = Guid.NewGuid() };
        _recoRepo.Setup(r => r.GetByIdAsync(recoId)).ReturnsAsync(reco);

        var result = await CreateSut().MarkAsReadAsync(UserId, recoId);

        result.Should().BeFalse();
    }

    // ── DismissAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DismissAsync_ValidOwner_SetsIsDismissedAndReturnsTrue()
    {
        var recoId = Guid.NewGuid();
        var reco = new Recommendation { Id = recoId, UserId = UserId, IsDismissed = false };
        _recoRepo.Setup(r => r.GetByIdAsync(recoId)).ReturnsAsync(reco);

        var result = await CreateSut().DismissAsync(UserId, recoId);

        result.Should().BeTrue();
        reco.IsDismissed.Should().BeTrue();
        _recoRepo.Verify(r => r.UpdateAsync(reco), Times.Once);
    }

    [Fact]
    public async Task DismissAsync_NotFound_ReturnsFalse()
    {
        _recoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Recommendation?)null);

        var result = await CreateSut().DismissAsync(UserId, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DismissAsync_WrongUser_ReturnsFalse()
    {
        var recoId = Guid.NewGuid();
        var reco = new Recommendation { Id = recoId, UserId = Guid.NewGuid() };
        _recoRepo.Setup(r => r.GetByIdAsync(recoId)).ReturnsAsync(reco);

        var result = await CreateSut().DismissAsync(UserId, recoId);

        result.Should().BeFalse();
    }
}