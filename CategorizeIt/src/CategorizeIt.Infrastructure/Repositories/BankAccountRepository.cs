using CategorizeIt.Application.Interfaces;
using CategorizeIt.Domain.Entities;
using CategorizeIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CategorizeIt.Infrastructure.Repositories;

public class BankAccountRepository : IBankAccountRepository
{
    private readonly ApplicationDbContext _context;

    public BankAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BankAccount>> GetByConnectionIdAsync(Guid bankConnectionId)
    {
        return await _context.BankAccounts
            .Where(a => a.BankConnectionId == bankConnectionId)
            .ToListAsync();
    }

    public async Task<BankAccount?> GetByIdAsync(Guid id)
    {
        return await _context.BankAccounts.FindAsync(id);
    }

    public async Task<BankAccount?> GetByIdentificationHashAsync(string hash)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.IdentificationHash == hash);
    }

    public async Task CreateAsync(BankAccount account)
    {
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BankAccount account)
    {
        _context.BankAccounts.Update(account);
        await _context.SaveChangesAsync();
    }
}