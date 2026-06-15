using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Abstractions;

/// <summary>
/// Persists and searches <see cref="DocumentChunk"/> vectors.
///
/// Two implementations ship with the platform:
///   • InMemoryVectorStore  — zero-config, perfect for local dev and demos.
///   • AzureAiSearchVectorStore — production, backed by Azure AI Search.
/// They are interchangeable because both honour this single contract.
/// </summary>
public interface IVectorStore
{
    /// <summary>Insert or update a batch of chunks.</summary>
    Task UpsertAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default);

    /// <summary>
    /// Return the <paramref name="topK"/> most similar chunks to
    /// <paramref name="queryEmbedding"/>, restricted to a single tenant.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        string tenantId,
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken ct = default);
}
