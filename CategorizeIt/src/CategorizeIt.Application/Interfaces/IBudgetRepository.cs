using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Interfaces;

public interface IBudgetRepository
{
    Task<IEnumerable<Budget>> GetByUserIdAsync(Guid userId);
    Task<Budget?> GetByIdAsync(Guid id);
    Task<Budget?> GetByUserAndCategoryAsync(Guid userId, Guid categoryId);
    Task CreateAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(Budget budget);
}