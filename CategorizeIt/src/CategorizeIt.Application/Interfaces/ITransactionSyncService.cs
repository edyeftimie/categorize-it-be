namespace CategorizeIt.Application.Interfaces;

public interface ITransactionSyncService
{
    Task<int> SyncAccountAsync(Guid userId, Guid bankAccountId, CancellationToken ct = default);
    Task<int> SyncAllForUserAsync(Guid userId, CancellationToken ct = default);
}