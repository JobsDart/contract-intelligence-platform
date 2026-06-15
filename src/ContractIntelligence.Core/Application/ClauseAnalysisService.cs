using System.Text.Json;
using System.Text.Json.Serialization;
using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Application;

/// <summary>
/// Uses the LLM to extract individual clauses from a contract and assign each one
/// a type and a risk rating. This powers the "clause breakdown" and "GDPR / risk"
/// views in the product.
///
/// The model is asked to return strict JSON which we deserialize into
/// <see cref="Clause"/> records. We are defensive about code fences and stray
/// prose because models occasionally wrap JSON in ```json blocks.
/// </summary>
public sealed class ClauseAnalysisService
{
    private readonly IChatService _chat;

    // Cap the amount of text we send so a 200-page contract can't blow the token
    // budget in the demo. Production would map-reduce over the whole document.
    private const int MaxChars = 24_000;

    public ClauseAnalysisService(IChatService chat) => _chat = chat;

    private const string SystemPrompt =
        """
        You are an expert contract analyst. Extract the distinct legal clauses from
        the contract text. For each clause return an object with these fields:
          "type"          one of: Payment, Termination, Liability, Confidentiality,
                          DataProtectionGdpr, IntellectualProperty, GoverningLaw,
                          Indemnity, Warranty, Other
          "summary"       a one-sentence plain-language summary
          "sourceText"    the most relevant verbatim sentence from the clause
          "pageNumber"    the page number (integer) where the clause appears
          "risk"          one of: Low, Medium, High
          "riskRationale" a short reason for the risk rating, focusing on legal /
                          GDPR / financial exposure
        Return ONLY a JSON array of these objects. No markdown, no commentary.
        """;

    public async Task<IReadOnlyList<Clause>> AnalyzeAsync(
        ParsedDocument document, CancellationToken ct = default)
    {
        // Flatten pages into a single annotated string so the model can see page numbers.
        var text = string.Join("\n\n",
            document.Pages.Select(p => $"[PAGE {p.PageNumber}]\n{p.Text}"));
        if (text.Length > MaxChars) text = text[..MaxChars];
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<Clause>();

        var raw = await _chat.CompleteAsync(SystemPrompt, text, ct);
        return Parse(raw);
    }

    private static IReadOnlyList<Clause> Parse(string raw)
    {
        var json = ExtractJsonArray(raw);
        if (json is null) return Array.Empty<Clause>();

        try
        {
            var dtos = JsonSerializer.Deserialize<List<ClauseDto>>(json, JsonOptions)
                       ?? new List<ClauseDto>();
            return dtos.Select(d => new Clause(
                Type: ParseEnum(d.Type, ClauseType.Other),
                Summary: d.Summary ?? string.Empty,
                SourceText: d.SourceText ?? string.Empty,
                PageNumber: d.PageNumber,
                Risk: ParseEnum(d.Risk, RiskLevel.Low),
                RiskRationale: d.RiskRationale ?? string.Empty)).ToList();
        }
        catch (JsonException)
        {
            // The model returned something we can't parse — fail soft, indexing still worked.
            return Array.Empty<Clause>();
        }
    }

    /// <summary>Pull the first JSON array out of a possibly-fenced model response.</summary>
    private static string? ExtractJsonArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
        => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>DTO mirroring the JSON the model returns. Kept private to the service.</summary>
    private sealed class ClauseDto
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("summary")] public string? Summary { get; set; }
        [JsonPropertyName("sourceText")] public string? SourceText { get; set; }
        [JsonPropertyName("pageNumber")] public int PageNumber { get; set; }
        [JsonPropertyName("risk")] public string? Risk { get; set; }
        [JsonPropertyName("riskRationale")] public string? RiskRationale { get; set; }
    }
}
