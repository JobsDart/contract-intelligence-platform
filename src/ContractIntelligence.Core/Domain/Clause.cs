namespace ContractIntelligence.Core.Domain;

/// <summary>
/// The categories of clause the platform recognises. Extend this enum to teach
/// the system new clause types — the extraction prompt is generated from these names.
/// </summary>
public enum ClauseType
{
    Payment,
    Termination,
    Liability,
    Confidentiality,
    DataProtectionGdpr,
    IntellectualProperty,
    GoverningLaw,
    Indemnity,
    Warranty,
    Other
}

/// <summary>Risk rating assigned to a clause by the analysis model.</summary>
public enum RiskLevel
{
    Low,
    Medium,
    High
}

/// <summary>
/// A single clause extracted from a contract, classified by type and scored for risk.
/// This is what powers the "GDPR risk" and "payment terms" views in the product.
/// </summary>
/// <param name="Type">Recognised category of the clause.</param>
/// <param name="Summary">One-sentence plain-language summary for the UI.</param>
/// <param name="SourceText">The verbatim text from the contract (for citation / audit).</param>
/// <param name="PageNumber">1-based page where the clause was found.</param>
/// <param name="Risk">Risk rating.</param>
/// <param name="RiskRationale">Why the model assigned that risk level.</param>
public sealed record Clause(
    ClauseType Type,
    string Summary,
    string SourceText,
    int PageNumber,
    RiskLevel Risk,
    string RiskRationale);
