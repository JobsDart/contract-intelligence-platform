using ContractIntelligence.Core.Application;

namespace ContractIntelligence.Api.Endpoints;

/// <summary>Natural-language Q&amp;A over the tenant's contracts (the RAG endpoint).</summary>
public static class QueryEndpoints
{
    public static void MapQueryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/query", async (
            AskRequest body, HttpRequest req, QueryService query, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Question))
                return Results.BadRequest("Question is required.");

            var tenant = TenantResolver.Resolve(req);
            var answer = await query.AskAsync(tenant, body.Question, body.TopK ?? 5, ct);
            return Results.Ok(answer);
        })
        .WithTags("Query")
        .WithName("Ask")
        .WithSummary("Ask a question; get a grounded answer with citations.");
    }
}
