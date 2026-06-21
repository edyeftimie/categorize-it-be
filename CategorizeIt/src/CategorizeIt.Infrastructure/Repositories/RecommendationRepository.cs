using CategorizeIt.Application.Interfaces;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CategorizeIt.Infrastructure.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    private readonly ApplicationDbContext _context;

    public RecommendationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Recommendation>> GetByUserIdAsync(Guid userId, bool includeRead = false, bool includeDismissed = false)
    {
        var query = _context.Recommendations
            .Where(r => r.UserId == userId)
            .Include(r => r.Category)
            .AsQueryable();

        if (!includeRead)
            query = query.Where(r => !r.IsRead);

        if (!includeDismissed)
            query = query.Where(r => !r.IsDismissed);

        return await query
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Recommendation?> GetByIdAsync(Guid id)
    {
        return await _context.Recommendations
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task CreateRangeAsync(IEnumerable<Recommendation> recommendations)
    {
        _context.Recommendations.AddRange(recommendations);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Recommendation recommendation)
    {
        _context.Recommendations.Update(recommendation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAllForUserAsync(Guid userId)
    {
        var recommendations = await _context.Recommendations
            .Where(r => r.UserId == userId)
            .ToListAsync();

        _context.Recommendations.RemoveRange(recommendations);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Recommendation>> GetByUserIdAndMonthAsync(Guid userId, int month, int year)
    {
        return await _context.Recommendations
            .Include(r => r.Category)
            .Where(r => r.UserId == userId
                    && r.CreatedAt.Month == month
                    && r.CreatedAt.Year == year)
            .ToListAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<Recommendation> recommendations)
    {
        _context.Recommendations.UpdateRange(recommendations);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Recommendation> recommendations)
    {
        _context.Recommendations.RemoveRange(recommendations);
        await _context.SaveChangesAsync();
    }
}