namespace Dominodo.Shared.Kernel;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
    bool IsSuperAdmin { get; }
}
