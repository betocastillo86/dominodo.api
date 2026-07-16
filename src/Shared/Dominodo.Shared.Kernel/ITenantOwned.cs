namespace Dominodo.Shared.Kernel;

public interface ITenantOwned
{
    Guid TenantId { get; }
}
