using CategorizeIt.Application.Models.BankConnections;

namespace CategorizeIt.Application.Interfaces;

public interface IBankConnectionService
{
    Task<InitiateAuthResult> InitiateAuthAsync(string aspspName, string aspspCountry, CancellationToken ct);
    Task<BankConnectionDto> HandleCallbackAsync(Guid userId, string code, CancellationToken ct);
    Task<IEnumerable<BankConnectionDto>> GetConnectionsAsync(Guid userId);
    Task<bool> DeleteConnectionAsync(Guid id);
}