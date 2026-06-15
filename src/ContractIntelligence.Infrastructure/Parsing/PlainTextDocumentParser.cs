using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Infrastructure.Parsing;

/// <summary>
/// Handles .txt and .md uploads. Treats the whole file as a single page.
/// Handy for quick demos and for ingesting exported/plain contracts.
/// </summary>
public sealed class PlainTextDocumentParser : IDocumentParser
{
    public bool CanParse(string fileName)
        => fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedDocument> ParseAsync(
        Stream content, string fileName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content);
        var text = await reader.ReadToEndAsync(ct);
        return new ParsedDocument(new[] { new ParsedPage(1, text) });
    }
}
