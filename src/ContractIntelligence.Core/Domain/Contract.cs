namespace ContractIntelligence.Core.Domain;

/// <summary>
/// Lifecycle state of a contract as it moves through the platform.
/// </summary>
public enum ContractStatus
{
    /// <summary>The file has been received but not yet processed.</summary>
    Uploaded,
    /// <summary>Text has been extracted, chunked, embedded and stored in the vector index.</summary>
    Indexed,
    /// <summary>The LLM has extracted and risk-scored the clauses.</summary>
    Analyzed,
    /// <summary>Processing failed; see <see cref="Contract.Error"/>.</summary>
    Failed
}

/// <summary>
/// Aggregate root. Represents a single uploaded contract document together with
/// everything the platform has learned about it (page count, extracted clauses,
/// processing status).
///
/// The raw text chunks + embeddings live in the vector store (see
/// <see cref="Abstractions.IVectorStore"/>) — not on the aggregate — because they
/// can be large and are queried independently of contract metadata.
/// </summary>
public sealed class Contract
{
    private readonly List<Clause> _clauses = new();

    public Contract(string fileName, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        Id = Guid.NewGuid();
        FileName = fileName;
        TenantId = tenantId;
        UploadedAtUtc = DateTimeOffset.UtcNow;
        Status = ContractStatus.Uploaded;
    }

    /// <summary>Stable identifier used everywhere (URLs, citations, vector metadata).</summary>
    public Guid Id { get; }

    /// <summary>Original file name as uploaded by the user.</summary>
    public string FileName { get; }

    /// <summary>
    /// Owning tenant. Every query is filtered by this value so one customer can
    /// never see another customer's contracts. This is the core multi-tenancy guard.
    /// </summary>
    public string TenantId { get; }

    public DateTimeOffset UploadedAtUtc { get; }

    public ContractStatus Status { get; private set; }

    public int PageCount { get; private set; }

    public string? Error { get; private set; }

    /// <summary>Clauses extracted and risk-scored by the LLM. Empty until analysed.</summary>
    public IReadOnlyList<Clause> Clauses => _clauses;

    public void MarkIndexed(int pageCount)
    {
        PageCount = pageCount;
        Status = ContractStatus.Indexed;
    }

    public void MarkAnalyzed(IEnumerable<Clause> clauses)
    {
        _clauses.Clear();
        _clauses.AddRange(clauses);
        Status = ContractStatus.Analyzed;
    }

    public void MarkFailed(string error)
    {
        Error = error;
        Status = ContractStatus.Failed;
    }
}
