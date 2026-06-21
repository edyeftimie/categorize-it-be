using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Transactions;
using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactions;
    private readonly ITransactionSyncService _syncService;

    public TransactionService(ITransactionRepository transactions, ITransactionSyncService syncService)
    {
        _transactions = transactions;
        _syncService = syncService;
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilters filters)
    {
        var transactions = await _transactions.GetByUserIdAsync(userId, filters);
        return transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            BankAccountId = t.BankAccountId,
            EntryReference = t.EntryReference,
            Amount = t.Amount,
            Currency = t.Currency,
            IsExpense = t.IsExpense,
            BookingDate = t.BookingDate,
            MerchantName = t.MerchantName,
            Description = t.Description,
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            CategoryColor = t.Category?.Color,
            CategoryIcon = t.Category?.Icon,
            IsManual = t.IsManual,
            IsRecurring = t.IsRecurring,
            CreatedAt = t.CreatedAt
        });
    }

    public async Task<Guid> CreateTransactionAsync(Guid userId, CreateTransactionRequest request)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = request.Amount,
            Currency = request.Currency,
            IsExpense = request.IsExpense,
            BookingDate = request.BookingDate,
            MerchantName = request.MerchantName,
            Description = request.Description,
            CategoryId = request.CategoryId,
            IsManual = true
        };

        await _transactions.CreateAsync(transaction);
        return transaction.Id;
    }

    public async Task<bool> RecategoriseAsync(Guid userId, Guid transactionId, Guid? categoryId)
    {
        var transaction = await _transactions.GetByIdAsync(transactionId);
        if (transaction == null || transaction.UserId != userId)
            return false;

        transaction.CategoryId = categoryId;
        await _transactions.UpdateAsync(transaction);
        return true;
    }

    public Task<int> SyncAllForUserAsync(Guid userId, CancellationToken ct)
    {
        return _syncService.SyncAllForUserAsync(userId, ct);
    }
}