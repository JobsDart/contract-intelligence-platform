using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Application;

/// <summary>
/// Splits parsed pages into overlapping, fixed-size text chunks.
///
/// Why chunk at all? Embedding models have a token limit and retrieval works best
/// on small, focused passages. Why overlap? So a sentence that straddles a chunk
/// boundary is still fully present in at least one chunk — this prevents answers
/// from being cut in half.
/// </summary>
public static class TextChunker
{
    /// <param name="pages">Parsed pages of a document.</param>
    /// <param name="maxChars">Target chunk size in characters (~250 tokens at 1k chars).</param>
    /// <param name="overlapChars">How many characters each chunk repeats from the previous one.</param>
    public static IReadOnlyList<(int PageNumber, int ChunkIndex, string Text)> Chunk(
        IReadOnlyList<ParsedPage> pages,
        int maxChars = 1000,
        int overlapChars = 150)
    {
        if (maxChars <= 0) throw new ArgumentOutOfRangeException(nameof(maxChars));
        if (overlapChars < 0 || overlapChars >= maxChars)
            throw new ArgumentOutOfRangeException(nameof(overlapChars));

        var result = new List<(int, int, string)>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var text = page.Text?.Trim() ?? string.Empty;
            if (text.Length == 0) continue;

            var start = 0;
            while (start < text.Length)
            {
                var length = Math.Min(maxChars, text.Length - start);
                var slice = text.Substring(start, length).Trim();
                if (slice.Length > 0)
                    result.Add((page.PageNumber, chunkIndex++, slice));

                if (start + length >= text.Length) break;
                start += maxChars - overlapChars; // step forward, keeping the overlap
            }
        }

        return result;
    }
}
