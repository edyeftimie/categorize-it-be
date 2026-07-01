using CategorizeIt.Domain.Entities;
using CategorizeIt.Domain.Enums;
using CategorizeIt.Infrastructure.Data;
using CategorizeIt.Infrastructure.Repositories;
using CategorizeIt.Application.Models.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CategorizeIt.Infrastructure.Tests;

// Shared helper: each test class gets a fresh in-memory DB
public abstract class InMemoryDbTest : IDisposable
{
    protected readonly ApplicationDbContext Db;

    protected InMemoryDbTest()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new ApplicationDbContext(opts);
    }

    public void Dispose() => Db.Dispose();

    protected User SeedUser(Guid? id = null)
    {
        var user = new User { Id = id ?? Guid.NewGuid(), Email = "u@test.com", Role = Role.User };
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    protected Category SeedCategory(string name = "Food & Dining")
    {
        var cat = new Category { Id = Guid.NewGuid(), Name = name, IsSystem = true };
        Db.Categories.Add(cat);
        Db.SaveChanges();
        return cat;
    }
}

// ── TransactionRepository ─────────────────────────────────────────────────────

public class TransactionRepositoryTests : InMemoryDbTest
{
    private TransactionRepository Repo => new(Db);

    private Transaction MakeTx(Guid userId, Guid? catId = null, bool isExpense = true,
        DateTime? date = null, string? entryRef = null, Guid? bankAccountId = null) => new()
    {
        Id            = Guid.NewGuid(),
        UserId        = userId,
        CategoryId    = catId,
        IsExpense     = isExpense,
        Amount        = 100m,
        Currency      = "RON",
        BookingDate   = date ?? new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        EntryReference = entryRef,
        BankAccountId = bankAccountId,
        IsManual      = false
    };

    [Fact]
    public async Task CreateAsync_PersistsTransaction()
    {
        var user = SeedUser();
        var tx   = MakeTx(user.Id);

        await Repo.CreateAsync(tx);

        Db.Transactions.Should().ContainSingle(t => t.Id == tx.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTransaction()
    {
        var user = SeedUser();
        var tx   = MakeTx(user.Id);
        await Repo.CreateAsync(tx);

        var result = await Repo.GetByIdAsync(tx.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tx.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await Repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesAreSaved()
    {
        var user = SeedUser();
        var tx   = MakeTx(user.Id);
        await Repo.CreateAsync(tx);

        tx.Amount = 999m;
        await Repo.UpdateAsync(tx);

        var stored = await Db.Transactions.FindAsync(tx.Id);
        stored!.Amount.Should().Be(999m);
    }

    [Fact]
    public async Task GetByUserIdAsync_SearchFilter_FiltersCorrectly()
    {
        var user = SeedUser();
        var tx1  = MakeTx(user.Id); tx1.MerchantName = "Starbucks";
        var tx2  = MakeTx(user.Id); tx2.MerchantName = "Netflix";
        Db.Transactions.AddRange(tx1, tx2);
        await Db.SaveChangesAsync();

        var filters = new TransactionFilters { Search = "Star", Page = 1, PageSize = 10 };
        var result  = await Repo.GetByUserIdAsync(user.Id, filters);

        result.Should().ContainSingle(t => t.MerchantName == "Starbucks");
    }

    [Fact]
    public async Task GetByUserIdAsync_MonthYearFilter_FiltersCorrectly()
    {
        var user = SeedUser();
        var jan  = MakeTx(user.Id, date: new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));
        var feb  = MakeTx(user.Id, date: new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        Db.Transactions.AddRange(jan, feb);
        await Db.SaveChangesAsync();

        var filters = new TransactionFilters { Month = 1, Year = 2025, Page = 1, PageSize = 10 };
        var result  = await Repo.GetByUserIdAsync(user.Id, filters);

        result.Should().ContainSingle(t => t.BookingDate.Month == 1);
    }

    [Fact]
    public async Task GetByUserIdAsync_IsExpenseFilter_FiltersCorrectly()
    {
        var user    = SeedUser();
        var expense = MakeTx(user.Id, isExpense: true);
        var income  = MakeTx(user.Id, isExpense: false);
        Db.Transactions.AddRange(expense, income);
        await Db.SaveChangesAsync();

        var filters = new TransactionFilters { IsExpense = true, Page = 1, PageSize = 10 };
        var result  = await Repo.GetByUserIdAsync(user.Id, filters);

        result.Should().AllSatisfy(t => t.IsExpense.Should().BeTrue());
    }

    [Fact]
    public async Task GetByUserIdAsync_CategoryIdFilter_FiltersCorrectly()
    {
        var user  = SeedUser();
        var cat   = SeedCategory();
        var txCat = MakeTx(user.Id, catId: cat.Id);
        var txNo  = MakeTx(user.Id);
        Db.Transactions.AddRange(txCat, txNo);
        await Db.SaveChangesAsync();

        var filters = new TransactionFilters { CategoryId = cat.Id, Page = 1, PageSize = 10 };
        var result  = await Repo.GetByUserIdAsync(user.Id, filters);

        result.Should().ContainSingle(t => t.CategoryId == cat.Id);
    }

    [Fact]
    public async Task AddRangeAsync_InsertsAll()
    {
        var user = SeedUser();
        var txs  = new[] { MakeTx(user.Id), MakeTx(user.Id) };

        await Repo.AddRangeAsync(txs);

        Db.Transactions.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetEntryReferencesByBankAccountAsync_ReturnsOnlyNonNullRefs()
    {
        var user      = SeedUser();
        var accId     = Guid.NewGuid();
        var withRef   = MakeTx(user.Id, bankAccountId: accId, entryRef: "ref-001");
        var withoutRef = MakeTx(user.Id, bankAccountId: accId, entryRef: null);
        var otherAcc  = MakeTx(user.Id, bankAccountId: Guid.NewGuid(), entryRef: "ref-other");
        Db.Transactions.AddRange(withRef, withoutRef, otherAcc);
        await Db.SaveChangesAsync();

        var refs = await Repo.GetEntryReferencesByBankAccountAsync(accId);

        refs.Should().BeEquivalentTo(new[] { "ref-001" });
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_CalculatesIncomAndExpensesSeparately()
    {
        var user    = SeedUser();
        var expense = MakeTx(user.Id, isExpense: true);  expense.Amount = 200m;
        var income  = MakeTx(user.Id, isExpense: false); income.Amount  = 500m;
        Db.Transactions.AddRange(expense, income);
        await Db.SaveChangesAsync();

        var (inc, exp) = await Repo.GetMonthlySummaryAsync(user.Id, 6, 2025);

        inc.Should().Be(500m);
        exp.Should().Be(200m);
    }

    [Fact]
    public async Task GetAllTimeBalanceAsync_ReturnsIncomeMinusExpenses()
    {
        var user    = SeedUser();
        var expense = MakeTx(user.Id, isExpense: true);  expense.Amount = 300m;
        var income  = MakeTx(user.Id, isExpense: false); income.Amount  = 1000m;
        Db.Transactions.AddRange(expense, income);
        await Db.SaveChangesAsync();

        var balance = await Repo.GetAllTimeBalanceAsync(user.Id);

        balance.Should().Be(700m);
    }

    [Fact]
    public async Task GetExpensesForMonthAsync_ReturnsOnlyExpensesForMonth()
    {
        var user  = SeedUser();
        var june  = MakeTx(user.Id, isExpense: true,  date: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var july  = MakeTx(user.Id, isExpense: true,  date: new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        var inc   = MakeTx(user.Id, isExpense: false, date: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        Db.Transactions.AddRange(june, july, inc);
        await Db.SaveChangesAsync();

        var result = await Repo.GetExpensesForMonthAsync(user.Id, 6, 2025);

        result.Should().ContainSingle(t => t.Id == june.Id);
    }
}

// ── BudgetRepository ──────────────────────────────────────────────────────────

public class BudgetRepositoryTests : InMemoryDbTest
{
    private BudgetRepository Repo => new(Db);

    private Budget MakeBudget(Guid userId, Guid catId) => new()
    {
        Id           = Guid.NewGuid(),
        UserId       = userId,
        CategoryId   = catId,
        MonthlyLimit = 500m,
        Currency     = "RON"
    };

    [Fact]
    public async Task CreateAsync_PersistsBudget()
    {
        var user   = SeedUser();
        var cat    = SeedCategory();
        var budget = MakeBudget(user.Id, cat.Id);

        await Repo.CreateAsync(budget);

        Db.Budgets.Should().ContainSingle(b => b.Id == budget.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyUserBudgets()
    {
        var u1  = SeedUser();
        var u2  = SeedUser();
        var cat = SeedCategory();
        Db.Budgets.Add(MakeBudget(u1.Id, cat.Id));
        Db.Budgets.Add(MakeBudget(u2.Id, cat.Id));
        await Db.SaveChangesAsync();

        var result = await Repo.GetByUserIdAsync(u1.Id);

        result.Should().ContainSingle(b => b.UserId == u1.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBudget()
    {
        var user   = SeedUser();
        var cat    = SeedCategory();
        var budget = MakeBudget(user.Id, cat.Id);
        await Repo.CreateAsync(budget);

        var result = await Repo.GetByIdAsync(budget.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(budget.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await Repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var user   = SeedUser();
        var cat    = SeedCategory();
        Db.Budgets.Add(MakeBudget(user.Id, cat.Id));
        await Db.SaveChangesAsync();

        var exists = await Repo.ExistsAsync(user.Id, cat.Id);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var user = SeedUser();
        var cat  = SeedCategory();

        var exists = await Repo.ExistsAsync(user.Id, cat.Id);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersisted()
    {
        var user   = SeedUser();
        var cat    = SeedCategory();
        var budget = MakeBudget(user.Id, cat.Id);
        await Repo.CreateAsync(budget);

        budget.MonthlyLimit = 999m;
        await Repo.UpdateAsync(budget);

        var stored = await Db.Budgets.FindAsync(budget.Id);
        stored!.MonthlyLimit.Should().Be(999m);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBudget()
    {
        var user   = SeedUser();
        var cat    = SeedCategory();
        var budget = MakeBudget(user.Id, cat.Id);
        await Repo.CreateAsync(budget);

        await Repo.DeleteAsync(budget);

        Db.Budgets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserAndCategoryAsync_ReturnsMatchingBudget()
    {
        var user   = SeedUser();
        var cat    = SeedCategory();
        var budget = MakeBudget(user.Id, cat.Id);
        await Repo.CreateAsync(budget);

        var result = await Repo.GetByUserAndCategoryAsync(user.Id, cat.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(budget.Id);
    }
}

// ── BankConnectionRepository ──────────────────────────────────────────────────

public class BankConnectionRepositoryTests : InMemoryDbTest
{
    private BankConnectionRepository Repo => new(Db);

    private BankConnection MakeConnection(Guid userId, string status = "AUTHORIZED") => new()
    {
        Id          = Guid.NewGuid(),
        UserId      = userId,
        SessionId   = Guid.NewGuid().ToString(),
        AspspName   = "BRD",
        AspspCountry = "RO",
        PsuType     = "personal",
        ValidUntil  = DateTime.UtcNow.AddDays(90),
        Status      = status
    };

    [Fact]
    public async Task CreateAsync_PersistsConnection()
    {
        var user = SeedUser();
        var conn = MakeConnection(user.Id);

        await Repo.CreateAsync(conn);

        Db.BankConnections.Should().ContainSingle(c => c.Id == conn.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ExcludesDisconnected()
    {
        var user   = SeedUser();
        var active = MakeConnection(user.Id, "AUTHORIZED");
        var dead   = MakeConnection(user.Id, "DISCONNECTED");
        Db.BankConnections.AddRange(active, dead);
        await Db.SaveChangesAsync();

        var result = await Repo.GetByUserIdAsync(user.Id);

        result.Should().ContainSingle(c => c.Id == active.Id);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesDisconnected()
    {
        var user = SeedUser();
        var conn = MakeConnection(user.Id, "DISCONNECTED");
        Db.BankConnections.Add(conn);
        await Db.SaveChangesAsync();

        var result = await Repo.GetByIdAsync(conn.Id);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await Repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeleteSetsDisconnected()
    {
        var user = SeedUser();
        var conn = MakeConnection(user.Id);
        await Repo.CreateAsync(conn);

        await Repo.DeleteAsync(conn);

        var stored = await Db.BankConnections.FindAsync(conn.Id);
        stored!.Status.Should().Be("DISCONNECTED");
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersisted()
    {
        var user = SeedUser();
        var conn = MakeConnection(user.Id);
        await Repo.CreateAsync(conn);

        conn.AspspName = "ING";
        await Repo.UpdateAsync(conn);

        var stored = await Db.BankConnections.FindAsync(conn.Id);
        stored!.AspspName.Should().Be("ING");
    }

    [Fact]
    public async Task GetBySessionIdAsync_ReturnsMatchingConnection()
    {
        var user = SeedUser();
        var conn = MakeConnection(user.Id);
        await Repo.CreateAsync(conn);

        var result = await Repo.GetBySessionIdAsync(conn.SessionId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(conn.Id);
    }

    [Fact]
    public async Task GetAccountsByIdentificationHashesForUserAsync_ReturnsMatchingAccounts()
    {
        var user = SeedUser();
        var conn = MakeConnection(user.Id);
        await Repo.CreateAsync(conn);

        var acc = new BankAccount
        {
            Id = Guid.NewGuid(), BankConnectionId = conn.Id,
            Uid = "uid-1", IdentificationHash = "hash-abc", Currency = "RON"
        };
        Db.BankAccounts.Add(acc);
        await Db.SaveChangesAsync();

        var result = await Repo.GetAccountsByIdentificationHashesForUserAsync(user.Id, new[] { "hash-abc" });

        result.Should().ContainSingle(a => a.IdentificationHash == "hash-abc");
    }

    [Fact]
    public async Task GetAccountsByIdentificationHashesForUserAsync_WrongUser_ReturnsEmpty()
    {
        var user  = SeedUser();
        var user2 = SeedUser();
        var conn  = MakeConnection(user.Id);
        await Repo.CreateAsync(conn);

        var acc = new BankAccount
        {
            Id = Guid.NewGuid(), BankConnectionId = conn.Id,
            Uid = "uid-1", IdentificationHash = "hash-abc", Currency = "RON"
        };
        Db.BankAccounts.Add(acc);
        await Db.SaveChangesAsync();

        var result = await Repo.GetAccountsByIdentificationHashesForUserAsync(user2.Id, new[] { "hash-abc" });

        result.Should().BeEmpty();
    }
}