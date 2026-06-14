using CategorizeIt.Application.Interfaces;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CategorizeIt.Infrastructure.Repositories;

public class BankConnectionRepository : IBankConnectionRepository
{
    private readonly ApplicationDbContext _context;

    public BankConnectionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BankConnection>> GetByUserIdAsync(Guid userId)
    {
        return await _context.BankConnections
            .Where(c => c.UserId == userId)
            .Include(c => c.BankAccounts)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<BankConnection?> GetByIdAsync(Guid id)
    {
        return await _context.BankConnections
            .Include(c => c.BankAccounts)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<BankConnection?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.BankConnections
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task CreateAsync(BankConnection connection)
    {
        _context.BankConnections.Add(connection);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BankConnection connection)
    {
        _context.BankConnections.Update(connection);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(BankConnection connection)
    {
        _context.BankConnections.Remove(connection);
        await _context.SaveChangesAsync();
    }
}