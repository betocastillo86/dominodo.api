namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated response for <c>GET /api/v1/roles/{id}</c>.
/// Mirrors <c>RoleDetailDto</c> by value; <c>Scope</c> arrives as a string
/// (e.g. <c>"Platform"</c> or <c>"Tenant"</c>).
/// </summary>
public sealed record RoleDetailModel
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public string Scope { get; init; } = default!;
    public IReadOnlyList<RolePermissionSummaryModel> Permissions { get; init; } = [];
}

/// <summary>Hand-replicated nested item in <see cref="RoleDetailModel.Permissions"/>.</summary>
public sealed record RolePermissionSummaryModel
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
}
