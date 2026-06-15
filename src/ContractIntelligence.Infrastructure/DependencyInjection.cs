using Azure;
using Azure.AI.OpenAI;
using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Application;
using ContractIntelligence.Infrastructure.Ai;
using ContractIntelligence.Infrastructure.Parsing;
using ContractIntelligence.Infrastructure.Storage;
using ContractIntelligence.Infrastructure.Vector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace ContractIntelligence.Infrastructure;

/// <summary>
/// Single composition root for the whole platform. The API calls
/// <c>services.AddContractIntelligence(configuration)</c> and everything is wired
/// from configuration — including the choice between in-memory and Azure backends.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddContractIntelligence(
        this IServiceCollection services, IConfiguration config)
    {
        // ---- Azure OpenAI (chat + embeddings) ----
        // Normalize to the base host (scheme://host/). The Azure OpenAI client appends
        // its own "/openai/deployments/.../...?api-version=" path, so any extra path the
        // user pasted (e.g. the Foundry "/openai/v1" surface) must be stripped — otherwise
        // Azure returns "api-version query parameter is not allowed when using /v1 path".
        var endpoint = NormalizeEndpoint(Required(config, "Ai:AzureOpenAI:Endpoint"));
        var apiKey = Required(config, "Ai:AzureOpenAI:ApiKey");
        var chatDeployment = config["Ai:AzureOpenAI:ChatDeployment"] ?? "gpt-4o";
        var embedDeployment = config["Ai:AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-large";

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        // Chat completion (SK) → adapted to our IChatService.
        IChatCompletionService skChat =
            new AzureOpenAIChatCompletionService(chatDeployment, azureClient);
        services.AddSingleton(skChat);
        services.AddSingleton<IChatService, SemanticKernelChatService>();

        // Embeddings via the modern Microsoft.Extensions.AI generator, registered by SK.
        // Our SemanticKernelEmbeddingGenerator adapts it to the Core abstraction.
        services.AddAzureOpenAIEmbeddingGenerator(embedDeployment, endpoint, apiKey);
        services.AddSingleton<IEmbeddingGenerator, SemanticKernelEmbeddingGenerator>();

        // ---- Document parsers (the ingestion service picks one per file type) ----
        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();

        // ---- Vector store: InMemory (default) or AzureAiSearch ----
        var vectorProvider = config["VectorStore:Provider"] ?? "InMemory";
        if (vectorProvider.Equals("AzureAiSearch", StringComparison.OrdinalIgnoreCase))
        {
            var searchEndpoint = Required(config, "VectorStore:AzureAiSearch:Endpoint");
            var searchKey = Required(config, "VectorStore:AzureAiSearch:ApiKey");
            var indexName = config["VectorStore:AzureAiSearch:IndexName"] ?? "contracts";
            var dimensions = int.TryParse(config["VectorStore:AzureAiSearch:Dimensions"], out var d) ? d : 3072;
            services.AddSingleton<IVectorStore>(
                new AzureAiSearchVectorStore(searchEndpoint, searchKey, indexName, dimensions));
        }
        else
        {
            services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        }

        // ---- Contract metadata store ----
        services.AddSingleton<IContractStore, InMemoryContractStore>();

        // ---- Application services (use cases) ----
        services.AddSingleton<ClauseAnalysisService>();
        services.AddSingleton<IngestionService>();
        services.AddSingleton<QueryService>();

        return services;
    }

    /// <summary>Reduce any Azure OpenAI / Foundry endpoint to its base "scheme://host/" form.</summary>
    private static string NormalizeEndpoint(string raw)
    {
        var uri = new Uri(raw.Trim(), UriKind.Absolute);
        return $"{uri.Scheme}://{uri.Authority}/";
    }

    private static string Required(IConfiguration config, string key)
        => config[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set it in appsettings.json, " +
                "user-secrets or an environment variable.");
}
