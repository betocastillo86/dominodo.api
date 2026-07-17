namespace Dominodo.E2E.Core.Context;

/// <summary>
/// Holds the "current" tenant slug for the executing test, injected into the
/// <c>X-Tenant</c> header by the tenant handler. Present but unused while
/// multitenancy is deferred: when no slug is set, the handler injects nothing
/// (so anonymous endpoints like registration work against the NullTenantDirectory).
/// </summary>
public static class AmbientTenantContext
{
    private static readonly AsyncLocal<string?> Slug = new();

    public static string? CurrentSlug
    {
        get => Slug.Value;
        set => Slug.Value = value;
    }

    public static void Clear()
    {
        Slug.Value = null;
    }
}
