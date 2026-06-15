using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Api.Endpoints;

/// <summary>API-facing view of a <see cref="Contract"/> (enums flattened to strings for clients).</summary>
public sealed record ContractResponse(
    Guid Id,
    string FileName,
    string Status,
    int PageCount,
    DateTimeOffset UploadedAtUtc,
    string? Error,
    IReadOnlyList<ClauseResponse> Clauses)
{
    public static ContractResponse From(Contract c) => new(
        c.Id,
        c.FileName,
        c.Status.ToString(),
        c.PageCount,
        c.UploadedAtUtc,
        c.Error,
        c.Clauses.Select(ClauseResponse.From).ToList());
}

/// <summary>API-facing view of a <see cref="Clause"/>.</summary>
public sealed record ClauseResponse(
    string Type,
    string Summary,
    string SourceText,
    int PageNumber,
    string Risk,
    string RiskRationale)
{
    public static ClauseResponse From(Clause c) => new(
        c.Type.ToString(),
        c.Summary,
        c.SourceText,
        c.PageNumber,
        c.Risk.ToString(),
        c.RiskRationale);
}

/// <summary>Request body for the Q&amp;A endpoint.</summary>
/// <param name="Question">The natural-language question.</param>
/// <param name="TopK">How many passages to retrieve (default 5).</param>
public sealed record AskRequest(string Question, int? TopK);
