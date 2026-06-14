using CategorizeIt.Application.Interfaces;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CategorizeIt.Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly ApplicationDbContext _context;

    public BudgetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Budget>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Budgets
            .Where(b => b.UserId == userId)
            .Include(b => b.Category)
            .ToListAsync();
    }

    public async Task<Budget?> GetByIdAsync(Guid id)
    {
        return await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Budget?> GetByUserAndCategoryAsync(Guid userId, Guid categoryId)
    {
        return await _context.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == categoryId);
    }

    public async Task CreateAsync(Budget budget)
    {
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Budget budget)
    {
        _context.Budgets.Update(budget);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Budget budget)
    {
        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid categoryId)
    {
        return await _context.Budgets
            .AnyAsync(b => b.UserId == userId && b.CategoryId == categoryId);
    }
}