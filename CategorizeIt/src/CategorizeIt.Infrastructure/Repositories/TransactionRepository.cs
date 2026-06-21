using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Transactions;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CategorizeIt.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Transaction>> GetByUserIdAsync(Guid userId, TransactionFilters filters)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(t =>
                (t.MerchantName != null && t.MerchantName.Contains(filters.Search)) ||
                (t.Description != null && t.Description.Contains(filters.Search)));

        if (filters.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filters.CategoryId);

        if (filters.Month.HasValue)
            query = query.Where(t => t.BookingDate.Month == filters.Month);

        if (filters.Year.HasValue)
            query = query.Where(t => t.BookingDate.Year == filters.Year);

        if (filters.IsExpense.HasValue)
            query = query.Where(t => t.IsExpense == filters.IsExpense);

        return await query
            .OrderByDescending(t => t.BookingDate)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<List<(Guid CategoryId, string CategoryName, string? CategoryColor, string? CategoryIcon, decimal Total)>>
        GetExpensesByCategoryAsync(Guid userId, int month, int year)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.IsExpense &&
                        t.CategoryId != null &&
                        t.BookingDate.Month == month && t.BookingDate.Year == year)
            .Include(t => t.Category)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Color, t.Category.Icon })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId!.Value,
                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                CategoryIcon = g.Key.Icon,
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x =>
                (x.CategoryId, x.CategoryName, x.CategoryColor, x.CategoryIcon, x.Total)).ToList());
    }

    public async Task<(decimal Income, decimal Expenses)> GetMonthlySummaryAsync(Guid userId, int month, int year)
    {
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.BookingDate.Month == month && t.BookingDate.Year == year)
            .ToListAsync();

        var income = transactions.Where(t => !t.IsExpense).Sum(t => t.Amount);
        var expenses = transactions.Where(t => t.IsExpense).Sum(t => t.Amount);

        return (income, expenses);
    }

    public async Task<decimal> GetAllTimeBalanceAsync(Guid userId)
    {
        var income = await _context.Transactions
            .Where(t => t.UserId == userId && !t.IsExpense)
            .SumAsync(t => t.Amount);

        var expenses = await _context.Transactions
            .Where(t => t.UserId == userId && t.IsExpense)
            .SumAsync(t => t.Amount);

        return income - expenses;
    }

    public async Task<List<(int Month, int Year, decimal Total)>> GetMonthlySeriesAsync(Guid userId, Guid categoryId, int months)
    {
        var from = DateTime.UtcNow.AddMonths(-months + 1);

        return await _context.Transactions
            .Where(t => t.UserId == userId && t.CategoryId == categoryId &&
                        t.IsExpense && t.BookingDate >= from)
            .GroupBy(t => new { t.BookingDate.Month, t.BookingDate.Year })
            .Select(g => new { g.Key.Month, g.Key.Year, Total = g.Sum(t => t.Amount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Month, x.Year, x.Total)).ToList());
    }
    
    public async Task<HashSet<string>> GetEntryReferencesByBankAccountAsync(Guid bankAccountId)
    {
        var refs = await _context.Transactions
            .Where(t => t.BankAccountId == bankAccountId && t.EntryReference != null)
            .Select(t => t.EntryReference!)
            .ToListAsync();
        return refs.ToHashSet();
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Transaction>> GetExpensesForMonthAsync(Guid userId, int month, int year)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.IsExpense &&
                        t.BookingDate.Month == month && t.BookingDate.Year == year)
            .ToListAsync();
    }
}