using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Core.Application;

/// <summary>
/// Orchestrates the full "upload → searchable + analysed" pipeline for one contract:
///
///   1. Parse the binary into per-page text.
///   2. Chunk the text and generate embeddings.
///   3. Upsert the chunks into the vector store (now searchable).
///   4. Run clause extraction + risk analysis over the text.
///   5. Persist the contract metadata.
///
/// This is the single entry point the API calls when a file is uploaded.
/// </summary>
public sealed class IngestionService
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IVectorStore _vectorStore;
    private readonly IContractStore _contractStore;
    private readonly ClauseAnalysisService _clauseAnalysis;

    public IngestionService(
        IEnumerable<IDocumentParser> parsers,
        IEmbeddingGenerator embeddings,
        IVectorStore vectorStore,
        IContractStore contractStore,
        ClauseAnalysisService clauseAnalysis)
    {
        _parsers = parsers;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _contractStore = contractStore;
        _clauseAnalysis = clauseAnalysis;
    }

    public async Task<Contract> IngestAsync(
        Stream content, string fileName, string tenantId, CancellationToken ct = default)
    {
        var contract = new Contract(fileName, tenantId);

        try
        {
            // 1. Parse — choose the first parser that recognises the file type.
            var parser = _parsers.FirstOrDefault(p => p.CanParse(fileName))
                ?? throw new NotSupportedException($"No parser available for '{fileName}'.");
            var parsed = await parser.ParseAsync(content, fileName, ct);

            // 2. Chunk + 3. Embed + 4. Upsert.
            var chunks = TextChunker.Chunk(parsed.Pages);
            if (chunks.Count > 0)
            {
                var embeddings = await _embeddings.GenerateAsync(
                    chunks.Select(c => c.Text).ToList(), ct);

                var documentChunks = chunks.Select((c, i) => new DocumentChunk(
                    Id: Guid.NewGuid(),
                    ContractId: contract.Id,
                    TenantId: tenantId,
                    FileName: fileName,
                    Text: c.Text,
                    PageNumber: c.PageNumber,
                    ChunkIndex: c.ChunkIndex,
                    Embedding: embeddings[i])).ToList();

                await _vectorStore.UpsertAsync(documentChunks, ct);
            }
            contract.MarkIndexed(parsed.PageCount);

            // 5. Clause extraction + risk analysis (best-effort; indexing already succeeded).
            var clauses = await _clauseAnalysis.AnalyzeAsync(parsed, ct);
            contract.MarkAnalyzed(clauses);
        }
        catch (Exception ex)
        {
            contract.MarkFailed(ex.Message);
        }

        await _contractStore.SaveAsync(contract, ct);
        return contract;
    }
}
