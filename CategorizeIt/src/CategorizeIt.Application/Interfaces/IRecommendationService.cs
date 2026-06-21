using CategorizeIt.Application.Models.Recommendations;

namespace CategorizeIt.Application.Interfaces;

public interface IRecommendationService
{
    Task GenerateForUserAsync(Guid userId, (int Month, int Year)? period, CancellationToken ct = default);
    Task<IEnumerable<RecommendationDto>> GetCurrentMonthAsync(Guid userId, bool includeRead = false, bool includeDismissed = false);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<bool> MarkAsReadAsync(Guid userId, Guid recommendationId);
    Task<bool> DismissAsync(Guid userId, Guid recommendationId);
}