using Dominodo.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Dominodo.Shared.Infrastructure.Auth;

// Ambient request-scoped caller identity, mirror of HttpTenantContext. Reads the subject the same
// way PermissionAuthorizationHandler does (NameIdentifier ?? "sub", Guid.TryParse). Fails closed.
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        TryGetUserId(out var id)
            ? id
            : throw new InvalidOperationException("No authenticated user for this request.");

    public bool IsAuthenticated => TryGetUserId(out _);

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return false;
        }

        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(subject, out userId);
    }
}
