using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ContractIntelligence.Infrastructure.Parsing;

/// <summary>
/// Extracts text from PDF files using PdfPig. Uses the content-order extractor,
/// which reconstructs natural reading order far better than raw glyph order.
/// </summary>
public sealed class PdfDocumentParser : IDocumentParser
{
    public bool CanParse(string fileName)
        => fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedDocument> ParseAsync(
        Stream content, string fileName, CancellationToken ct = default)
    {
        // PdfPig needs a seekable stream; copy to memory to be safe with upload streams.
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;

        var pages = new List<ParsedPage>();
        using var pdf = PdfDocument.Open(ms);
        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            var text = ContentOrderTextExtractor.GetText(page) ?? string.Empty;
            pages.Add(new ParsedPage(page.Number, text));
        }

        return new ParsedDocument(pages);
    }
}
