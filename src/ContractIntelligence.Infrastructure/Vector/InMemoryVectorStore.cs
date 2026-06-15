using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Infrastructure.Vector;

/// <summary>
/// A zero-dependency vector store that keeps chunks in memory and ranks them by
/// cosine similarity. Perfect for local development, demos and tests — no Azure
/// resource required. Data is lost on restart (by design).
///
/// In production this is swapped for <see cref="AzureAiSearchVectorStore"/> via
/// configuration; the rest of the app is unaware of the change.
/// </summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<DocumentChunk> _chunks = new();
    private readonly Lock _gate = new();

    public Task UpsertAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _chunks.AddRange(chunks);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchHit>> SearchAsync(
        string tenantId, ReadOnlyMemory<float> queryEmbedding, int topK, CancellationToken ct = default)
    {
        List<DocumentChunk> snapshot;
        lock (_gate)
        {
            // Tenant isolation enforced here — never compare across tenants.
            snapshot = _chunks.Where(c => c.TenantId == tenantId).ToList();
        }

        // Capture the embedding (a normal struct) — a Span cannot be used inside a lambda.
        var query = queryEmbedding;
        var ranked = snapshot
            .Select(c => new SearchHit(c, CosineSimilarity(query.Span, c.Embedding.Span)))
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchHit>>(ranked);
    }

    /// <summary>Standard cosine similarity between two equal-length vectors.</summary>
    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
