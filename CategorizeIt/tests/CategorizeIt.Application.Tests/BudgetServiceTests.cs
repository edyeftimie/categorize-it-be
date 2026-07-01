using Xunit;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Budgets;
using CategorizeIt.Application.Services;
using CategorizeIt.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CategorizeIt.Application.Tests;

public class BudgetServiceTests
{
    private readonly Mock<IBudgetRepository>      _budgetRepo = new();
    private readonly Mock<ITransactionRepository> _txRepo     = new();
    private readonly Mock<IRecommendationService> _recoSvc    = new();

    private BudgetService CreateSut() => new(
        _budgetRepo.Object,
        _txRepo.Object,
        _recoSvc.Object,
        NullLogger<BudgetService>.Instance);

    private static readonly Guid UserId   = Guid.NewGuid();
    private static readonly Guid CatId    = Guid.NewGuid();
    private static readonly Guid BudgetId = Guid.NewGuid();

    private Budget MakeBudget(Guid? userId = null) => new()
    {
        Id           = BudgetId,
        UserId       = userId ?? UserId,
        CategoryId   = CatId,
        MonthlyLimit = 500m,
        Currency     = "RON",
        Category     = new Category { Id = CatId, Name = "Food", Icon = "🍕", Color = "#fff" }
    };

    // ── GetBudgetsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBudgetsAsync_ReturnsMappedDtos()
    {
        var budget = MakeBudget();
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget> { budget });
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)>
               {
                   (CatId, "Food", null, null, 200m)
               });

        var result = (await CreateSut().GetBudgetsAsync(UserId)).ToList();

        result.Should().HaveCount(1);
        result[0].CategoryName.Should().Be("Food");
        result[0].MonthlyLimit.Should().Be(500m);
        result[0].AmountSpent.Should().Be(200m);
    }

    [Fact]
    public async Task GetBudgetsAsync_NoBudgets_ReturnsEmpty()
    {
        _budgetRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Budget>());
        _txRepo.Setup(r => r.GetExpensesByCategoryAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(new List<(Guid, string, string?, string?, decimal)>());

        var result = await CreateSut().GetBudgetsAsync(UserId);

        result.Should().BeEmpty();
    }

    // ── CreateBudgetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBudgetAsync_NewBudget_CreatesBudgetAndReturnsId()
    {
        _budgetRepo.Setup(r => r.ExistsAsync(UserId, CatId)).ReturnsAsync(false);

        var request = new CreateBudgetRequest { CategoryId = CatId, MonthlyLimit = 300m, Currency = "RON" };
        var result  = await CreateSut().CreateBudgetAsync(UserId, request);

        result.Should().NotBeNull();
        _budgetRepo.Verify(r => r.CreateAsync(It.Is<Budget>(b => b.CategoryId == CatId && b.MonthlyLimit == 300m)), Times.Once);
        _recoSvc.Verify(r => r.GenerateForUserAsync(UserId, null, default), Times.Once);
    }

    [Fact]
    public async Task CreateBudgetAsync_BudgetAlreadyExists_ReturnsNull()
    {
        _budgetRepo.Setup(r => r.ExistsAsync(UserId, CatId)).ReturnsAsync(true);

        var request = new CreateBudgetRequest { CategoryId = CatId, MonthlyLimit = 300m };
        var result  = await CreateSut().CreateBudgetAsync(UserId, request);

        result.Should().BeNull();
        _budgetRepo.Verify(r => r.CreateAsync(It.IsAny<Budget>()), Times.Never);
    }

    [Fact]
    public async Task CreateBudgetAsync_RecommendationThrows_DoesNotPropagate()
    {
        _budgetRepo.Setup(r => r.ExistsAsync(UserId, CatId)).ReturnsAsync(false);
        _recoSvc.Setup(r => r.GenerateForUserAsync(UserId, null, default))
                .ThrowsAsync(new Exception("reco error"));

        var request = new CreateBudgetRequest { CategoryId = CatId, MonthlyLimit = 100m };
        var act = async () => await CreateSut().CreateBudgetAsync(UserId, request);

        await act.Should().NotThrowAsync();
    }

    // ── UpdateBudgetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBudgetAsync_ValidOwner_UpdatesLimitAndReturnsTrue()
    {
        var budget = MakeBudget();
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync(budget);

        var request = new UpdateBudgetRequest { MonthlyLimit = 800m };
        var result  = await CreateSut().UpdateBudgetAsync(UserId, BudgetId, request);

        result.Should().BeTrue();
        budget.MonthlyLimit.Should().Be(800m);
        _budgetRepo.Verify(r => r.UpdateAsync(budget), Times.Once);
        _recoSvc.Verify(r => r.GenerateForUserAsync(UserId, null, default), Times.Once);
    }

    [Fact]
    public async Task UpdateBudgetAsync_BudgetNotFound_ReturnsFalse()
    {
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync((Budget?)null);

        var result = await CreateSut().UpdateBudgetAsync(UserId, BudgetId, new UpdateBudgetRequest());

        result.Should().BeFalse();
        _budgetRepo.Verify(r => r.UpdateAsync(It.IsAny<Budget>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBudgetAsync_WrongUser_ReturnsFalse()
    {
        var budget = MakeBudget(Guid.NewGuid());
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync(budget);

        var result = await CreateSut().UpdateBudgetAsync(UserId, BudgetId, new UpdateBudgetRequest());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBudgetAsync_RecommendationThrows_DoesNotPropagate()
    {
        var budget = MakeBudget();
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync(budget);
        _recoSvc.Setup(r => r.GenerateForUserAsync(UserId, null, default))
                .ThrowsAsync(new Exception("reco error"));

        var act = async () => await CreateSut().UpdateBudgetAsync(UserId, BudgetId, new UpdateBudgetRequest { MonthlyLimit = 1m });

        await act.Should().NotThrowAsync();
    }

    // ── DeleteBudgetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBudgetAsync_ValidOwner_DeletesAndReturnsTrue()
    {
        var budget = MakeBudget();
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync(budget);

        var result = await CreateSut().DeleteBudgetAsync(UserId, BudgetId);

        result.Should().BeTrue();
        _budgetRepo.Verify(r => r.DeleteAsync(budget), Times.Once);
        _recoSvc.Verify(r => r.GenerateForUserAsync(UserId, null, default), Times.Once);
    }

    [Fact]
    public async Task DeleteBudgetAsync_BudgetNotFound_ReturnsFalse()
    {
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync((Budget?)null);

        var result = await CreateSut().DeleteBudgetAsync(UserId, BudgetId);

        result.Should().BeFalse();
        _budgetRepo.Verify(r => r.DeleteAsync(It.IsAny<Budget>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBudgetAsync_WrongUser_ReturnsFalse()
    {
        var budget = MakeBudget(Guid.NewGuid());
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync(budget);

        var result = await CreateSut().DeleteBudgetAsync(UserId, BudgetId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBudgetAsync_RecommendationThrows_DoesNotPropagate()
    {
        var budget = MakeBudget();
        _budgetRepo.Setup(r => r.GetByIdAsync(BudgetId)).ReturnsAsync(budget);
        _recoSvc.Setup(r => r.GenerateForUserAsync(UserId, null, default))
                .ThrowsAsync(new Exception("reco error"));

        var act = async () => await CreateSut().DeleteBudgetAsync(UserId, BudgetId);

        await act.Should().NotThrowAsync();
    }
}