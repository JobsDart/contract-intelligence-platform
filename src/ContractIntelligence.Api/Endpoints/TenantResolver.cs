namespace ContractIntelligence.Api.Endpoints;

/// <summary>
/// Resolves the calling tenant from the <c>X-Tenant-Id</c> request header.
/// Falls back to a shared "demo-tenant" so the public demo works with no setup.
///
/// In production this would come from the authenticated principal (JWT claim),
/// not a header — but the rest of the code is already tenant-aware, so that change
/// is isolated to this one method.
/// </summary>
public static class TenantResolver
{
    public const string HeaderName = "X-Tenant-Id";
    public const string DefaultTenant = "demo-tenant";

    public static string Resolve(HttpRequest request)
        => request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : DefaultTenant;
}
