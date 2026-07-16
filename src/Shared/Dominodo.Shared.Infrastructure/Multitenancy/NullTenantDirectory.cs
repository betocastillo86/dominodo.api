using Dominodo.Shared.Abstractions;

namespace Dominodo.Shared.Infrastructure.Multitenancy;

internal sealed class NullTenantDirectory : ITenantDirectory
{
    public Task<Guid?> ResolveSlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult<Guid?>(null);
}
