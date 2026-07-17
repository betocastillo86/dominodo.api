namespace Dominodo.Shared.Abstractions;

// Resolves a user's effective permission codes for the current request, server-side.
// Effective set = permissions of the user's Platform-scope roles (always) ∪ permissions of the
// user's role in the given tenant (when tenantId is present — Membership). Implemented in the host
// so it may reach the Users module facade; the authorization handler depends only on this port.
// See docs/architecture/12-permission-authorization.md.
public interface IPermissionProvider
{
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
