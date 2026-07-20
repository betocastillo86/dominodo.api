namespace Dominodo.Shared.Kernel;

public interface ICurrentUser
{
    // Throws if not authenticated — call sites are [Authorize]-guarded.
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
