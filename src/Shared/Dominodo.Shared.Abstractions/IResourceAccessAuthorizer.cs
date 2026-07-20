namespace Dominodo.Shared.Abstractions;

// Resolves "caller holds the permission (RBAC) OR the caller owns this resource". Ownership is a
// same-module data check the caller supplies as a delegate; it runs ONLY when the permission is
// absent (short-circuits the owner query for permitted callers). Fails closed (false) if no caller.
// Returns a plain access boolean — the caller shapes the transport error (403 vs leak-safe 404).
// See docs/architecture/12-permission-authorization.md.
public interface IResourceAccessAuthorizer
{
    Task<bool> HasAccessAsync(string permission, Func<Guid, bool> isOwner, CancellationToken ct = default);

    Task<bool> HasAccessAsync(string permission, Func<Guid, CancellationToken, Task<bool>> isOwnerAsync, CancellationToken ct = default);
}
