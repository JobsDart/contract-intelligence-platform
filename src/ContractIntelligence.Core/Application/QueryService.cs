using System.Text;
using System.Text.RegularExpressions;
using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Application;

/// <summary>
/// The Retrieval-Augmented Generation (RAG) pipeline:
///
///   question → embed → vector search (tenant-scoped) → build grounded prompt →
///   LLM answer with [n] citation markers → map markers back to source passages.
///
/// The model is instructed to answer ONLY from retrieved context, which is what
/// keeps answers grounded and prevents hallucination.
/// </summary>
public sealed partial class QueryService
{
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IVectorStore _vectorStore;
    private readonly IChatService _chat;

    public QueryService(IEmbeddingGenerator embeddings, IVectorStore vectorStore, IChatService chat)
    {
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _chat = chat;
    }

    private const string SystemPrompt =
        """
        You are a contract-analysis assistant for an enterprise legal team.
        Answer the user's question using ONLY the numbered context passages provided.
        Cite every fact using the passage number in square brackets, e.g. [1] or [2].
        If the answer is not contained in the context, reply exactly:
        "I could not find that information in the provided contracts."
        Be precise and concise. Never invent clause text or numbers.
        """;

    public async Task<AnswerResult> AskAsync(
        string tenantId, string question, int topK = 5, CancellationToken ct = default)
    {
        // 1. Embed the question and retrieve the most relevant chunks for THIS tenant.
        var queryEmbedding = (await _embeddings.GenerateAsync(new[] { question }, ct))[0];
        var hits = await _vectorStore.SearchAsync(tenantId, queryEmbedding, topK, ct);

        if (hits.Count == 0)
            return new AnswerResult(
                "I could not find that information in the provided contracts.",
                Array.Empty<Citation>());

        // 2. Build a numbered context block the model can cite against.
        var context = new StringBuilder();
        for (var i = 0; i < hits.Count; i++)
        {
            var c = hits[i].Chunk;
            context.AppendLine($"[{i + 1}] (file: {c.FileName}, page {c.PageNumber})");
            context.AppendLine(c.Text);
            context.AppendLine();
        }

        var userPrompt = $"""
            Context passages:
            {context}
            Question: {question}
            """;

        // 3. Generate the grounded answer.
        var answer = await _chat.CompleteAsync(SystemPrompt, userPrompt, ct);

        // 4. Map the [n] markers the model actually used back to citations.
        var citations = ResolveCitations(answer, hits);
        return new AnswerResult(answer, citations);
    }

    /// <summary>
    /// Returns citations for the passage numbers referenced in the answer.
    /// If the model cited nothing explicitly, fall back to all retrieved hits.
    /// </summary>
    private static IReadOnlyList<Citation> ResolveCitations(string answer, IReadOnlyList<SearchHit> hits)
    {
        var referenced = CitationMarker().Matches(answer)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Where(n => n >= 1 && n <= hits.Count)
            .Distinct()
            .ToList();

        var chosen = referenced.Count > 0
            ? referenced.Select(n => hits[n - 1])
            : hits;

        return chosen.Select(h => new Citation(
            h.Chunk.ContractId,
            h.Chunk.FileName,
            h.Chunk.PageNumber,
            Snippet(h.Chunk.Text))).ToList();
    }

    private static string Snippet(string text, int max = 240)
        => text.Length <= max ? text : text[..max] + "…";

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex CitationMarker();
}
