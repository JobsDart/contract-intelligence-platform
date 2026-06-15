using ContractIntelligence.Api.Endpoints;
using ContractIntelligence.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Wire the entire platform from configuration (see Infrastructure/DependencyInjection.cs).
builder.Services.AddContractIntelligence(builder.Configuration);

// OpenAPI document for tooling / Swagger-style clients, available at /openapi/v1.json.
builder.Services.AddOpenApi();

// Permissive CORS so a separately-hosted SPA (or a recruiter's browser) can call the API.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.MapOpenApi();
app.UseCors();

// Serve the built-in single-page UI from wwwroot (index.html).
app.UseDefaultFiles();
app.UseStaticFiles();

// REST endpoints.
app.MapContractEndpoints();
app.MapQueryEndpoints();

app.Run();
