using Xunit;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.EnableBanking;
using CategorizeIt.Application.Services;
using CategorizeIt.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CategorizeIt.Application.Tests;

public class TransactionSyncServiceTests
{
    private readonly Mock<IEnableBankingClient>      _ebClient      = new();
    private readonly Mock<IBankAccountRepository>    _bankAccounts  = new();
    private readonly Mock<IBankConnectionRepository> _bankConns     = new();
    private readonly Mock<ITransactionRepository>    _txRepo        = new();
    private readonly Mock<ICategoryRepository>       _catRepo       = new();
    private readonly Mock<IMccCategoriser>           _mccCategoriser = new();
    private readonly Mock<IRecommendationService>    _recoService   = new();

    private TransactionSyncService CreateSut() => new(
        _ebClient.Object,
        _bankAccounts.Object,
        _bankConns.Object,
        _txRepo.Object,
        _catRepo.Object,
        _mccCategoriser.Object,
        _recoService.Object,
        NullLogger<TransactionSyncService>.Instance);

    private static readonly Guid UserId        = Guid.NewGuid();
    private static readonly Guid AccountId     = Guid.NewGuid();
    private static readonly string AccountUid  = "acc-uid-001";

    private BankAccount MakeAccount(DateTime? lastSyncedAt = null) => new()
    {
        Id           = AccountId,
        BankConnectionId = Guid.NewGuid(),
        Uid          = AccountUid,
        IdentificationHash = "hash",
        LastSyncedAt = lastSyncedAt
    };

    private static EnableBankingTransactionDto MakeTxDto(string? entryRef, string bookingDate = "2025-01-15",
        string mcc = "5411", string amount = "50.00", string indicator = "DBIT") => new()
    {
        EntryReference       = entryRef,
        MerchantCategoryCode = mcc,
        BookingDate          = bookingDate,
        CreditDebitIndicator = indicator,
        TransactionAmount    = new TransactionAmountDto { Amount = amount, Currency = "RON" }
    };

    private void SetupCategories(params (string Name, Guid Id)[] cats)
    {
        var list = cats.Select(c => new Category { Id = c.Id, Name = c.Name }).ToList();
        _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
    }

    // ── SyncAccountAsync — new transactions inserted ──────────────────────────

    [Fact]
    public async Task SyncAccountAsync_NewTransactions_InsertsThemAll()
    {
        var account = MakeAccount();
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        var page = new TransactionsPageDto
        {
            Transactions = new List<EnableBankingTransactionDto>
            {
                MakeTxDto("ref-001"),
                MakeTxDto("ref-002"),
            }
        };
        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(page);

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());

        var catId = Guid.NewGuid();
        SetupCategories(("Food & Dining", catId));
        _mccCategoriser.Setup(m => m.Classify(It.IsAny<string?>()))
                       .Returns(("Food & Dining", Domain.Enums.NeedWantSavings.Need));

        var sut = CreateSut();
        var inserted = await sut.SyncAccountAsync(UserId, AccountId);

        inserted.Should().Be(2);
        _txRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Transaction>>(t => t.Count() == 2)), Times.Once);
        _bankAccounts.Verify(r => r.UpdateAsync(account), Times.Once);
    }

    // ── SyncAccountAsync — dedup: duplicate skipped ───────────────────────────

    [Fact]
    public async Task SyncAccountAsync_DuplicateEntryReference_SkipsDuplicate()
    {
        var account = MakeAccount();
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        var page = new TransactionsPageDto
        {
            Transactions = new List<EnableBankingTransactionDto>
            {
                MakeTxDto("ref-exists"),
                MakeTxDto("ref-new"),
            }
        };
        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(page);

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string> { "ref-exists" });

        var catId = Guid.NewGuid();
        SetupCategories(("Food & Dining", catId));
        _mccCategoriser.Setup(m => m.Classify(It.IsAny<string?>()))
                       .Returns(("Food & Dining", Domain.Enums.NeedWantSavings.Need));

        var sut = CreateSut();
        var inserted = await sut.SyncAccountAsync(UserId, AccountId);

        inserted.Should().Be(1);
        _txRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Transaction>>(t => t.Count() == 1)), Times.Once);
    }

    // ── SyncAccountAsync — dedup: null EntryReference never skipped ──────────

    [Fact]
    public async Task SyncAccountAsync_NullEntryReference_IsNeverTreatedAsDuplicate()
    {
        var account = MakeAccount();
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        var page = new TransactionsPageDto
        {
            Transactions = new List<EnableBankingTransactionDto> { MakeTxDto(null) }
        };
        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(page);

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());

        SetupCategories(("Other", Guid.NewGuid()));
        _mccCategoriser.Setup(m => m.Classify(It.IsAny<string?>()))
                       .Returns(("Other", Domain.Enums.NeedWantSavings.Uncategorised));

        var inserted = await CreateSut().SyncAccountAsync(UserId, AccountId);

        inserted.Should().Be(1);
    }

    // ── SyncAccountAsync — empty page ─────────────────────────────────────────

    [Fact]
    public async Task SyncAccountAsync_EmptyPage_InsertsNothingAndUpdatesLastSynced()
    {
        var account = MakeAccount();
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TransactionsPageDto { Transactions = new List<EnableBankingTransactionDto>() });

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());

        SetupCategories();

        var inserted = await CreateSut().SyncAccountAsync(UserId, AccountId);

        inserted.Should().Be(0);
        _txRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>()), Times.Never);
        _bankAccounts.Verify(r => r.UpdateAsync(account), Times.Once);
    }

    // ── SyncAccountAsync — pagination / continuation ──────────────────────────

    [Fact]
    public async Task SyncAccountAsync_PaginatedPages_FetchesAllPagesAndInsertsAll()
    {
        var account = MakeAccount();
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        var page1 = new TransactionsPageDto
        {
            Transactions    = new List<EnableBankingTransactionDto> { MakeTxDto("ref-p1") },
            ContinuationKey = "key-2"
        };
        var page2 = new TransactionsPageDto
        {
            Transactions    = new List<EnableBankingTransactionDto> { MakeTxDto("ref-p2") },
            ContinuationKey = null
        };

        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(page1);
        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, "key-2", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(page2);

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());

        SetupCategories(("Food & Dining", Guid.NewGuid()));
        _mccCategoriser.Setup(m => m.Classify(It.IsAny<string?>()))
                       .Returns(("Food & Dining", Domain.Enums.NeedWantSavings.Need));

        var inserted = await CreateSut().SyncAccountAsync(UserId, AccountId);

        inserted.Should().Be(2);
    }

    // ── SyncAccountAsync — lastSyncedAt used as dateFrom ─────────────────────

    [Fact]
    public async Task SyncAccountAsync_WithLastSyncedAt_PassesDateFromToClient()
    {
        var lastSync = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var account  = MakeAccount(lastSync);
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TransactionsPageDto());

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());
        SetupCategories();

        await CreateSut().SyncAccountAsync(UserId, AccountId);

        // dateFrom = lastSyncedAt - 2 days = 2025-01-08
        _ebClient.Verify(c => c.GetAccountTransactionsAsync(AccountUid, "2025-01-08", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAccountAsync_WithoutLastSyncedAt_PassesNullDateFrom()
    {
        var account = MakeAccount(null);
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TransactionsPageDto());

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());
        SetupCategories();

        await CreateSut().SyncAccountAsync(UserId, AccountId);

        _ebClient.Verify(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── SyncAccountAsync — MCC classification applied ─────────────────────────

    [Fact]
    public async Task SyncAccountAsync_KnownMcc_AssignsCategoryIdToTransaction()
    {
        var account = MakeAccount();
        var catId   = Guid.NewGuid();
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(account);

        _ebClient.Setup(c => c.GetAccountTransactionsAsync(AccountUid, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TransactionsPageDto
                 {
                     Transactions = new List<EnableBankingTransactionDto> { MakeTxDto("ref-1", mcc: "5411") }
                 });

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(AccountId))
               .ReturnsAsync(new HashSet<string>());

        SetupCategories(("Food & Dining", catId));
        _mccCategoriser.Setup(m => m.Classify("5411")).Returns(("Food & Dining", Domain.Enums.NeedWantSavings.Need));

        await CreateSut().SyncAccountAsync(UserId, AccountId);

        _txRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Transaction>>(
            list => list.Single().CategoryId == catId)), Times.Once);
    }

    // ── SyncAccountAsync — account not found throws ───────────────────────────

    [Fact]
    public async Task SyncAccountAsync_AccountNotFound_ThrowsInvalidOperationException()
    {
        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync((BankAccount?)null);

        var act = async () => await CreateSut().SyncAccountAsync(UserId, AccountId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── SyncAllForUserAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SyncAllForUserAsync_MultipleConnections_SumsInsertedCount()
    {
        var acc1 = MakeAccount();
        var acc2Id = Guid.NewGuid();
        var acc2 = new BankAccount { Id = acc2Id, BankConnectionId = Guid.NewGuid(), Uid = "acc-uid-002", IdentificationHash = "h2" };

        var conn1 = new BankConnection { Id = Guid.NewGuid(), UserId = UserId, BankAccounts = new List<BankAccount> { acc1 } };
        var conn2 = new BankConnection { Id = Guid.NewGuid(), UserId = UserId, BankAccounts = new List<BankAccount> { acc2 } };

        _bankConns.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<BankConnection> { conn1, conn2 });

        _bankAccounts.Setup(r => r.GetByIdAsync(AccountId)).ReturnsAsync(acc1);
        _bankAccounts.Setup(r => r.GetByIdAsync(acc2Id)).ReturnsAsync(acc2);

        foreach (var id in new[] { AccountId, acc2Id })
        {
            _ebClient.Setup(c => c.GetAccountTransactionsAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new TransactionsPageDto
                     {
                         Transactions = new List<EnableBankingTransactionDto> { MakeTxDto($"ref-{id}") }
                     });
        }

        _txRepo.Setup(r => r.GetEntryReferencesByBankAccountAsync(It.IsAny<Guid>()))
               .ReturnsAsync(new HashSet<string>());

        SetupCategories(("Other", Guid.NewGuid()));
        _mccCategoriser.Setup(m => m.Classify(It.IsAny<string?>()))
                       .Returns(("Other", Domain.Enums.NeedWantSavings.Uncategorised));

        var total = await CreateSut().SyncAllForUserAsync(UserId);

        total.Should().Be(2);
        _recoService.Verify(r => r.GenerateForUserAsync(UserId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAllForUserAsync_RecommendationThrows_DoesNotPropagateException()
    {
        _bankConns.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<BankConnection>());
        _recoService.Setup(r => r.GenerateForUserAsync(UserId, null, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("reco failure"));

        var act = async () => await CreateSut().SyncAllForUserAsync(UserId);

        await act.Should().NotThrowAsync();
    }
}