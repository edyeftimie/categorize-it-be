using CategorizeIt.Application.Interfaces;
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

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, Guid? categoryId = null)
    {
        var query = _context.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.BookingDate >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.BookingDate <= to.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        return await query.OrderByDescending(t => t.BookingDate).ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAndMonthAsync(Guid userId, int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        return await _context.Transactions
            .Where(t => t.UserId == userId && t.BookingDate >= from && t.BookingDate <= to)
            .Include(t => t.Category)
            .OrderByDescending(t => t.BookingDate)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> ExistsByEntryReferenceAsync(Guid userId, string entryReference)
    {
        return await _context.Transactions
            .AnyAsync(t => t.UserId == userId && t.EntryReference == entryReference);
    }

    public async Task CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task CreateRangeAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }
}