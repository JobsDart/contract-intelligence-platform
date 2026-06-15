namespace ContractIntelligence.Core.Abstractions;

/// <summary>
/// Turns text into embedding vectors. Backed by Azure OpenAI
/// (text-embedding-3-large) in production; can be swapped for a local model.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generate one embedding per input string. The returned list is index-aligned
    /// with <paramref name="texts"/>.
    /// </summary>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
