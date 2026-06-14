using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, Guid? categoryId = null);
    Task<IEnumerable<Transaction>> GetByUserIdAndMonthAsync(Guid userId, int year, int month);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<bool> ExistsByEntryReferenceAsync(Guid userId, string entryReference);
    Task CreateAsync(Transaction transaction);
    Task CreateRangeAsync(IEnumerable<Transaction> transactions);
    Task UpdateAsync(Transaction transaction);
}