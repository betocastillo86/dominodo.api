namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/tenants/{id}/status</c>. Mirrors the API's
/// <c>ChangeTenantStatusRequest</c>. <c>Status</c> must parse to the <c>TenantStatus</c> enum
/// ("Onboarding", "Active" or "Suspended").
/// </summary>
public sealed record ChangeTenantStatusModel
{
    public string Status { get; init; } = default!;
}
