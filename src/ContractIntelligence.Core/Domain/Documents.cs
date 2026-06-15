namespace ContractIntelligence.Core.Domain;

/// <summary>
/// Result of parsing a binary document (PDF, TXT, …) into plain text, one entry per page.
/// Produced by <see cref="Abstractions.IDocumentParser"/>.
/// </summary>
/// <param name="Pages">Extracted pages in document order.</param>
public sealed record ParsedDocument(IReadOnlyList<ParsedPage> Pages)
{
    public int PageCount => Pages.Count;
}

/// <param name="PageNumber">1-based page number.</param>
/// <param name="Text">Plain text content of the page.</param>
public sealed record ParsedPage(int PageNumber, string Text);

/// <summary>
/// A retrievable unit of contract text plus its embedding vector. This is the
/// atomic record stored in and returned from the vector index.
///
/// Chunks are deliberately small (≈1 000 characters) so that a search returns
/// tightly-scoped, citable passages rather than whole pages.
/// </summary>
/// <param name="Id">Unique chunk id.</param>
/// <param name="ContractId">Parent contract.</param>
/// <param name="TenantId">Owning tenant — used to filter every search.</param>
/// <param name="FileName">Parent file name, denormalised so citations need no extra lookup.</param>
/// <param name="Text">The chunk text.</param>
/// <param name="PageNumber">Page the chunk came from (for citations).</param>
/// <param name="ChunkIndex">Ordinal position of the chunk within the contract.</param>
/// <param name="Embedding">The vector representation of <paramref name="Text"/>.</param>
public sealed record DocumentChunk(
    Guid Id,
    Guid ContractId,
    string TenantId,
    string FileName,
    string Text,
    int PageNumber,
    int ChunkIndex,
    ReadOnlyMemory<float> Embedding);
