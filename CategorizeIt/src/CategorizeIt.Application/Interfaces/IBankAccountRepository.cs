using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Interfaces;

public interface IBankAccountRepository
{
    Task<IEnumerable<BankAccount>> GetByConnectionIdAsync(Guid bankConnectionId);
    Task<BankAccount?> GetByIdAsync(Guid id);
    Task<BankAccount?> GetByIdentificationHashAsync(string hash);
    Task CreateAsync(BankAccount account);
    Task UpdateAsync(BankAccount account);
}