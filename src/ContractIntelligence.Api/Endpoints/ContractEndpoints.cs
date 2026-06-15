using ContractIntelligence.Core.Abstractions;
using ContractIntelligence.Core.Application;

namespace ContractIntelligence.Api.Endpoints;

/// <summary>Upload and browse contracts.</summary>
public static class ContractEndpoints
{
    public static void MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contracts").WithTags("Contracts");

        // POST /api/contracts  — multipart upload; runs the full ingestion pipeline.
        group.MapPost("/", async (
            IFormFile file, HttpRequest req, IngestionService ingestion, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            var tenant = TenantResolver.Resolve(req);
            await using var stream = file.OpenReadStream();
            var contract = await ingestion.IngestAsync(stream, file.FileName, tenant, ct);
            return Results.Ok(ContractResponse.From(contract));
        })
        .DisableAntiforgery() // multipart upload from the SPA / API clients
        .WithName("UploadContract")
        .WithSummary("Upload a contract (PDF/TXT/MD) and analyse it.");

        // GET /api/contracts — list this tenant's contracts.
        group.MapGet("/", async (HttpRequest req, IContractStore store, CancellationToken ct) =>
        {
            var tenant = TenantResolver.Resolve(req);
            var list = await store.ListAsync(tenant, ct);
            return Results.Ok(list.Select(ContractResponse.From));
        })
        .WithName("ListContracts");

        // GET /api/contracts/{id} — full detail incl. extracted clauses.
        group.MapGet("/{id:guid}", async (
            Guid id, HttpRequest req, IContractStore store, CancellationToken ct) =>
        {
            var tenant = TenantResolver.Resolve(req);
            var contract = await store.GetAsync(tenant, id, ct);
            return contract is null
                ? Results.NotFound()
                : Results.Ok(ContractResponse.From(contract));
        })
        .WithName("GetContract");
    }
}
