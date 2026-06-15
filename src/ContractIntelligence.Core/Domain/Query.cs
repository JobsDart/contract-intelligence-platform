namespace ContractIntelligence.Core.Domain;

/// <summary>A single chunk returned from a vector search, with its similarity score.</summary>
/// <param name="Chunk">The matched chunk.</param>
/// <param name="Score">Cosine similarity in the range 0..1 (higher is more relevant).</param>
public sealed record SearchHit(DocumentChunk Chunk, double Score);

/// <summary>
/// A pointer back to the exact place in a source contract that an answer was
/// derived from. This is what makes the product trustworthy — every answer is
/// grounded in a citable passage the user can verify.
/// </summary>
/// <param name="ContractId">Source contract.</param>
/// <param name="FileName">Source file name.</param>
/// <param name="PageNumber">Page the snippet was taken from.</param>
/// <param name="Snippet">The supporting text.</param>
public sealed record Citation(Guid ContractId, string FileName, int PageNumber, string Snippet);

/// <summary>
/// The final answer to a user's question: the generated text plus the citations
/// that ground it. Returned by <see cref="Application.QueryService"/>.
/// </summary>
/// <param name="Answer">Natural-language answer produced by the LLM.</param>
/// <param name="Citations">Supporting passages, in the order the model referenced them.</param>
public sealed record AnswerResult(string Answer, IReadOnlyList<Citation> Citations);
