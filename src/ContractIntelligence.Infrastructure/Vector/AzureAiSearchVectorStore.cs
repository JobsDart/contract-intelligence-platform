using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Domain;

namespace ContractIntelligence.Infrastructure.Vector;

/// <summary>
/// Production vector store backed by Azure AI Search. Uses HNSW vector search with
/// a tenant filter, so retrieval is fast at scale and strictly tenant-isolated.
///
/// The index is created automatically on first use if it does not already exist.
/// </summary>
public sealed class AzureAiSearchVectorStore : IVectorStore
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private readonly int _dimensions;
    private bool _ensured;

    private const string VectorProfile = "vector-profile";
    private const string VectorAlgorithm = "hnsw-config";

    /// <param name="endpoint">e.g. https://my-search.search.windows.net</param>
    /// <param name="apiKey">Admin key.</param>
    /// <param name="indexName">Target index name.</param>
    /// <param name="dimensions">Embedding dimensions (3072 for text-embedding-3-large).</param>
    public AzureAiSearchVectorStore(string endpoint, string apiKey, string indexName, int dimensions)
    {
        var credential = new AzureKeyCredential(apiKey);
        var uri = new Uri(endpoint);
        _indexClient = new SearchIndexClient(uri, credential);
        _searchClient = new SearchClient(uri, indexName, credential);
        _indexName = indexName;
        _dimensions = dimensions;
    }

    public async Task UpsertAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);

        var documents = chunks.Select(c => new SearchDocument
        {
            ["id"] = c.Id.ToString(),
            ["contractId"] = c.ContractId.ToString(),
            ["tenantId"] = c.TenantId,
            ["fileName"] = c.FileName,
            ["text"] = c.Text,
            ["pageNumber"] = c.PageNumber,
            ["chunkIndex"] = c.ChunkIndex,
            ["embedding"] = c.Embedding.ToArray()
        });

        await _searchClient.MergeOrUploadDocumentsAsync(documents, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string tenantId, ReadOnlyMemory<float> queryEmbedding, int topK, CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);

        var options = new SearchOptions
        {
            Size = topK,
            // Tenant isolation enforced at the index level.
            Filter = $"tenantId eq '{tenantId.Replace("'", "''")}'",
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryEmbedding)
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { "embedding" }
                    }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>("*", options, ct);

        var hits = new List<SearchHit>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var d = result.Document;
            var chunk = new DocumentChunk(
                Id: Guid.Parse(d.GetString("id")),
                ContractId: Guid.Parse(d.GetString("contractId")),
                TenantId: d.GetString("tenantId"),
                FileName: d.GetString("fileName"),
                Text: d.GetString("text"),
                PageNumber: d.GetInt32("pageNumber") ?? 0,
                ChunkIndex: d.GetInt32("chunkIndex") ?? 0,
                Embedding: ReadOnlyMemory<float>.Empty);
            hits.Add(new SearchHit(chunk, result.Score ?? 0));
        }
        return hits;
    }

    /// <summary>Create the index (idempotently) the first time the store is used.</summary>
    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (_ensured) return;

        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("contractId", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("tenantId", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("fileName"),
                new SearchableField("text"),
                new SimpleField("pageNumber", SearchFieldDataType.Int32),
                new SimpleField("chunkIndex", SearchFieldDataType.Int32),
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _dimensions,
                    VectorSearchProfileName = VectorProfile
                }
            },
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile(VectorProfile, VectorAlgorithm) },
                Algorithms = { new HnswAlgorithmConfiguration(VectorAlgorithm) }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
        _ensured = true;
    }
}
