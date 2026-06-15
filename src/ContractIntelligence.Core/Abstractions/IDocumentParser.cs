using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Abstractions;

/// <summary>
/// Extracts plain text (page by page) from an uploaded document.
/// Implemented in the Infrastructure layer (e.g. PdfPig for PDFs).
/// </summary>
public interface IDocumentParser
{
    /// <summary>Returns true if this parser can handle the given file name / extension.</summary>
    bool CanParse(string fileName);

    /// <summary>Parse the supplied stream into per-page text.</summary>
    Task<ParsedDocument> ParseAsync(Stream content, string fileName, CancellationToken ct = default);
}
