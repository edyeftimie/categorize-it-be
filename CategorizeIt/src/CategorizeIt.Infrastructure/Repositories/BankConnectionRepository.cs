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

    // Excludes DISCONNECTED — only active connections shown to user
    public async Task<IEnumerable<BankConnection>> GetByUserIdAsync(Guid userId)
    {
        return await _context.BankConnections
            .Where(c => c.UserId == userId && c.Status != "DISCONNECTED")
            .Include(c => c.BankAccounts)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    // No status filter — needed for ownership checks on any connection (incl. disconnected)
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

    // Searches across ALL user connections (including DISCONNECTED) by account IdentificationHash.
    // IdentificationHash is STABLE across reconnects (Uid changes per session), so it's the
    // correct key for reconnect-reactivation. Used during reconnect to find existing accounts.
    public async Task<List<BankAccount>> GetAccountsByIdentificationHashesForUserAsync(
        Guid userId, IEnumerable<string> hashes)
    {
        var hashList = hashes.Where(h => h != null).ToList();
        return await _context.BankAccounts
            .Include(a => a.BankConnection)
            .Where(a => a.BankConnection.UserId == userId
                        && a.IdentificationHash != null
                        && hashList.Contains(a.IdentificationHash))
            .ToListAsync();
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

    // Soft-delete: marks DISCONNECTED, keeps accounts + transactions intact
    public async Task DeleteAsync(BankConnection connection)
    {
        connection.Status = "DISCONNECTED";
        _context.BankConnections.Update(connection);
        await _context.SaveChangesAsync();
    }
}