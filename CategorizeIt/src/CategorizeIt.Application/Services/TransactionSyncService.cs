using System.Globalization;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.EnableBanking;
using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Services;

public class TransactionSyncService : ITransactionSyncService
{
    private readonly IEnableBankingClient _enableBanking;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly IBankConnectionRepository _bankConnections;
    private readonly ITransactionRepository _transactions;
    
    private readonly ICategoryRepository _categories;
    private readonly IMccCategoriser _mccCategoriser;

    private readonly IRecommendationService _recommendationService;

    public TransactionSyncService(
        IEnableBankingClient enableBanking,
        IBankAccountRepository bankAccounts,
        IBankConnectionRepository bankConnections,
        ITransactionRepository transactions,
        ICategoryRepository categories,
        IMccCategoriser mccCategorizer,
        IRecommendationService recommendationService)
    {
        _enableBanking = enableBanking;
        _bankAccounts = bankAccounts;
        _bankConnections = bankConnections;
        _transactions = transactions;
        _categories = categories;
        _mccCategoriser = mccCategorizer;
        _recommendationService = recommendationService;
    }

    public async Task<int> SyncAccountAsync(Guid userId, Guid bankAccountId, CancellationToken ct = default)
    {
        // Step 1: load account, decide mode
        var account = await _bankAccounts.GetByIdAsync(bankAccountId)
            ?? throw new InvalidOperationException($"BankAccount {bankAccountId} not found.");

        var dateFrom = account.LastSyncedAt.HasValue
            ? account.LastSyncedAt.Value.AddDays(-2).ToString("yyyy-MM-dd")
            : null;

        // Step 2: fetch all pages
        var allFetched = new List<EnableBankingTransactionDto>();
        string? continuationKey = null;

        do
        {
            var page = await _enableBanking.GetAccountTransactionsAsync(
                account.Uid, dateFrom, continuationKey, ct);

            allFetched.AddRange(page.Transactions);
            continuationKey = page.ContinuationKey;
        }
        while (continuationKey != null);

        // Step 3+4: resolve categories and deduplicate
        var existingRefs = await _transactions.GetEntryReferencesByBankAccountAsync(bankAccountId);

        var categoryList = await _categories.GetAllAsync();
        var categoryMap = categoryList.ToDictionary(c => c.Name, c => c.Id);

        var newEntities = new List<Transaction>();

        foreach (var t in allFetched)
        {
            if (t.EntryReference != null && existingRefs.Contains(t.EntryReference))
                continue;

            Guid? categoryId = null;
            if (!string.IsNullOrEmpty(t.MerchantCategoryCode) ||
                t.MerchantCategoryCode == null)
            {
                var (categoryName, _) = _mccCategoriser.Classify(t.MerchantCategoryCode);
                if (categoryMap.TryGetValue(categoryName, out var resolvedId))
                    categoryId = resolvedId;
            }

            var bookingDate = DateTime.SpecifyKind(
                DateTime.ParseExact(t.BookingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTimeKind.Utc);

            newEntities.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BankAccountId = bankAccountId,
                EntryReference = t.EntryReference,
                Amount = decimal.Parse(t.TransactionAmount.Amount, CultureInfo.InvariantCulture),
                Currency = t.TransactionAmount.Currency,
                IsExpense = t.CreditDebitIndicator == "DBIT",
                BookingDate = bookingDate,
                MerchantName = t.Creditor?.Name ?? t.Debtor?.Name,
                MerchantCategoryCode = t.MerchantCategoryCode,
                Description = t.RemittanceInformation?.Count > 0
                    ? string.Join(" ", t.RemittanceInformation)
                    : null,
                CategoryId = categoryId,
                IsManual = false
            });
        }

        // Step 5: save new
        if (newEntities.Count > 0)
            await _transactions.AddRangeAsync(newEntities);

        // Step 6: update LastSyncedAt
        account.LastSyncedAt = DateTime.UtcNow;
        await _bankAccounts.UpdateAsync(account);

        // Step 7: return count
        return newEntities.Count;
    }

    public async Task<int> SyncAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var connections = await _bankConnections.GetByUserIdAsync(userId);
        var total = 0;

        foreach (var connection in connections)
        {
            foreach (var account in connection.BankAccounts)
            {
                total += await SyncAccountAsync(userId, account.Id, ct);
            }
        }

        try
        {
            await _recommendationService.GenerateForUserAsync(userId, null, ct);
        }
        catch (Exception ex)
        {
            //add later logging here
        }

        return total;
    }
}