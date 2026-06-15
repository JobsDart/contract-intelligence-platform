using System.Collections.Concurrent;
using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Infrastructure.Storage;

/// <summary>
/// In-memory store for contract metadata, keyed by tenant. Replace with EF Core +
/// Azure SQL for production durability — the <see cref="IContractStore"/> contract
/// stays identical, so nothing else changes.
/// </summary>
public sealed class InMemoryContractStore : IContractStore
{
    // tenantId -> (contractId -> Contract)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Contract>> _byTenant = new();

    public Task SaveAsync(Contract contract, CancellationToken ct = default)
    {
        var tenant = _byTenant.GetOrAdd(contract.TenantId, _ => new());
        tenant[contract.Id] = contract;
        return Task.CompletedTask;
    }

    public Task<Contract?> GetAsync(string tenantId, Guid id, CancellationToken ct = default)
    {
        Contract? found = null;
        if (_byTenant.TryGetValue(tenantId, out var tenant))
            tenant.TryGetValue(id, out found);
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<Contract>> ListAsync(string tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<Contract> list = _byTenant.TryGetValue(tenantId, out var tenant)
            ? tenant.Values.OrderByDescending(c => c.UploadedAtUtc).ToList()
            : Array.Empty<Contract>();
        return Task.FromResult(list);
    }
}
