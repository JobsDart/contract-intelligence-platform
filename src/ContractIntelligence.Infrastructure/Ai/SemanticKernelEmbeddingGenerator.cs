using Microsoft.Extensions.AI;

namespace ContractIntelligence.Infrastructure.Ai;

/// <summary>
/// Adapts the modern Microsoft.Extensions.AI embedding generator
/// (<see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>) to our small, dependency-free
/// <see cref="Core.Abstractions.IEmbeddingGenerator"/> abstraction.
///
/// Note the two interfaces share the name <c>IEmbeddingGenerator</c> but live in
/// different namespaces — the Core one is qualified explicitly below to avoid clashing
/// with the Microsoft.Extensions.AI marker interface.
/// </summary>
public sealed class SemanticKernelEmbeddingGenerator : Core.Abstractions.IEmbeddingGenerator
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _inner;

    public SemanticKernelEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> inner)
        => _inner = inner;

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var embeddings = await _inner.GenerateAsync(texts, cancellationToken: ct);
        return embeddings.Select(e => e.Vector).ToList();
    }
}
