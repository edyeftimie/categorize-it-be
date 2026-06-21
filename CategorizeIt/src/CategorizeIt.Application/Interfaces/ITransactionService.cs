using CategorizeIt.Application.Models.Transactions;

namespace CategorizeIt.Application.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid userId, TransactionFilters filters);
    Task<Guid> CreateTransactionAsync(Guid userId, CreateTransactionRequest request);
    Task<bool> RecategoriseAsync(Guid userId, Guid transactionId, Guid? categoryId);
    Task<int> SyncAllForUserAsync(Guid userId, CancellationToken ct);
}