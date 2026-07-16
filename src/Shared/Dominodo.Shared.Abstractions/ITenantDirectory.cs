namespace Dominodo.Shared.Abstractions;

public interface ITenantDirectory
{
    Task<Guid?> ResolveSlugAsync(string slug, CancellationToken cancellationToken = default);
}
