using CategorizeIt.Application.Models.Dashboard;

namespace CategorizeIt.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(Guid userId, int? month, int? year);
    Task<IEnumerable<MonthlyAmountDto>> GetMonthlySeriesAsync(Guid userId, Guid categoryId, int months);
}