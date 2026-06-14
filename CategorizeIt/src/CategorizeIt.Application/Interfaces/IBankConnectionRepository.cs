using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Interfaces;

public interface IBankConnectionRepository
{
    Task<IEnumerable<BankConnection>> GetByUserIdAsync(Guid userId);
    Task<BankConnection?> GetByIdAsync(Guid id);
    Task<BankConnection?> GetBySessionIdAsync(string sessionId);
    Task CreateAsync(BankConnection connection);
    Task UpdateAsync(BankConnection connection);
    Task DeleteAsync(BankConnection connection);
}