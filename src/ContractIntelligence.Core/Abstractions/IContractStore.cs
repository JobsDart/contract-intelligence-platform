using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Abstractions;

/// <summary>
/// Stores contract *metadata* (not the text chunks — those live in the vector store).
/// In-memory for the demo; swap for EF Core + Azure SQL in production.
/// </summary>
public interface IContractStore
{
    Task SaveAsync(Contract contract, CancellationToken ct = default);

    Task<Contract?> GetAsync(string tenantId, Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Contract>> ListAsync(string tenantId, CancellationToken ct = default);
}
