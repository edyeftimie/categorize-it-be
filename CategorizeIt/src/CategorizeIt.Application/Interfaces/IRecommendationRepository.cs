using CategorizeIt.Domain.Entities;

namespace CategorizeIt.Application.Interfaces;

public interface IRecommendationRepository
{
    Task<IEnumerable<Recommendation>> GetByUserIdAsync(Guid userId, bool includeRead = false, bool includeDismissed = false);
    Task<Recommendation?> GetByIdAsync(Guid id);
    Task CreateRangeAsync(IEnumerable<Recommendation> recommendations);
    Task UpdateAsync(Recommendation recommendation);
    Task DeleteAllForUserAsync(Guid userId);
}