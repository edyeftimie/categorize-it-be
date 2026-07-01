using Xunit;
using CategorizeIt.Domain.Constants;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Domain.Enums;
using FluentAssertions;

namespace CategorizeIt.Domain.Tests;

public class DomainEntityTests
{
    // App Constants
    [Fact]
    public void AppConstants_Jwt_HasExpectedDefaults()
    {
        AppConstants.Jwt.DefaultExpirationInMinutes.Should().Be(60);
        AppConstants.Jwt.RefreshTokenExpirationInDays.Should().Be(7);
    }

    [Fact]
    public void AppConstants_Validation_HasExpectedLengths()
    {
        AppConstants.Validation.EmailMaxLength.Should().Be(256);
        AppConstants.Validation.NameMaxLength.Should().Be(100);
        AppConstants.Validation.PasswordMinLength.Should().Be(8);
    }

    // NeedWantSavings
    [Fact]
    public void NeedWantSavings_AllExpectedValuesExist()
    {
        var values = Enum.GetValues<NeedWantSavings>();
        values.Should().Contain(NeedWantSavings.Need);
        values.Should().Contain(NeedWantSavings.Want);
        values.Should().Contain(NeedWantSavings.Savings);
        values.Should().Contain(NeedWantSavings.Uncategorised);
        values.Should().Contain(NeedWantSavings.Excluded);
    }

    // Recommendation Type
    [Fact]
    public void RecommendationType_AllExpectedValuesExist()
    {
        var values = Enum.GetValues<RecommendationType>();
        values.Should().Contain(RecommendationType.Overspend);
        values.Should().Contain(RecommendationType.TrendUp);
        values.Should().Contain(RecommendationType.TrendDown);
        values.Should().Contain(RecommendationType.SavingsTip);
        values.Should().Contain(RecommendationType.NoBudget);
    }

    // Role
    [Fact]
    public void Role_UserIsZero_AdminIsOne()
    {
        ((int)Role.User).Should().Be(0);
        ((int)Role.Admin).Should().Be(1);
    }

    // User
    [Fact]
    public void User_DefaultValues_AreCorrect()
    {
        var user = new User();
        user.Email.Should().Be(string.Empty);
        user.Role.Should().Be(Role.User);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_AssignedProperties_AreRetained()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id           = id,
            Email        = "test@example.com",
            Username     = "tester",
            PasswordHash = "hash",
            Role         = Role.Admin,
            GoogleId     = "g-123"
        };

        user.Id.Should().Be(id);
        user.Email.Should().Be("test@example.com");
        user.Username.Should().Be("tester");
        user.Role.Should().Be(Role.Admin);
        user.GoogleId.Should().Be("g-123");
    }

    // Transaction
    [Fact]
    public void Transaction_DefaultCurrency_IsRON()
    {
        new Transaction().Currency.Should().Be("RON");
    }

    [Fact]
    public void Transaction_IsExpenseFalse_ByDefault()
    {
        new Transaction().IsExpense.Should().BeFalse();
    }

    [Fact]
    public void Transaction_IsManualFalse_ByDefault()
    {
        new Transaction().IsManual.Should().BeFalse();
    }

    [Fact]
    public void Transaction_CreatedAt_IsApproximatelyNow()
    {
        new Transaction().CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Budget
    [Fact]
    public void Budget_DefaultCurrency_IsRON()
    {
        new Budget().Currency.Should().Be("RON");
    }

    [Fact]
    public void Budget_CreatedAt_IsApproximatelyNow()
    {
        new Budget().CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Bank Connection
    [Fact]
    public void BankConnection_DefaultStatus_IsActive()
    {
        new BankConnection().Status.Should().Be("Active");
    }

    [Fact]
    public void BankConnection_DefaultCountry_IsRO()
    {
        new BankConnection().AspspCountry.Should().Be("RO");
    }

    [Fact]
    public void BankConnection_BankAccounts_InitialisedEmpty()
    {
        new BankConnection().BankAccounts.Should().NotBeNull().And.BeEmpty();
    }

    // Bank Account
    [Fact]
    public void BankAccount_DefaultCurrency_IsRON()
    {
        new BankAccount().Currency.Should().Be("RON");
    }

    [Fact]
    public void BankAccount_LastSyncedAt_IsNullByDefault()
    {
        new BankAccount().LastSyncedAt.Should().BeNull();
    }

    [Fact]
    public void BankAccount_Transactions_InitialisedEmpty()
    {
        new BankAccount().Transactions.Should().NotBeNull().And.BeEmpty();
    }

    // Recommendation
    [Fact]
    public void Recommendation_IsReadAndIsDismissed_FalseByDefault()
    {
        var r = new Recommendation();
        r.IsRead.Should().BeFalse();
        r.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void Recommendation_CategoryId_NullByDefault()
    {
        new Recommendation().CategoryId.Should().BeNull();
    }

    // Category
    [Fact]
    public void Category_Collections_InitialisedEmpty()
    {
        var cat = new Category();
        cat.Transactions.Should().NotBeNull().And.BeEmpty();
        cat.Budgets.Should().NotBeNull().And.BeEmpty();
        cat.Recommendations.Should().NotBeNull().And.BeEmpty();
    }
}