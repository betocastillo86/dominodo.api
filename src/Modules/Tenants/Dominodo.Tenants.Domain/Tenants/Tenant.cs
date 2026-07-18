using System.Text.RegularExpressions;
using Dominodo.Shared.Kernel;
using Dominodo.Tenants.Domain.Tenants.Events;

namespace Dominodo.Tenants.Domain.Tenants;

// The residential complex (conjunto) registry — system-level, NOT ITenantOwned: this aggregate IS
// the tenant. Its Id is the tenant_id carried in the claim; its immutable Slug is what the X-Tenant
// header carries and ITenantDirectory resolves to that Id (domain-model §2.1, plan Phase 2).
public sealed partial class Tenant : AggregateRoot
{
    private readonly List<TenantFeature> _features = new();

    private Tenant() { } // EF Core

    private Tenant(
        Guid id,
        string slug,
        string name,
        TenantType type,
        string address,
        string city,
        string country,
        string? legalId) : base(id)
    {
        Slug = slug;
        Name = name;
        Type = type;
        Address = address;
        City = city;
        Country = country;
        LegalId = legalId;
        Status = TenantStatus.Onboarding;
    }

    // Immutable after creation — slugs can be renamed only by re-creating; the header/claim depend on it.
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? LegalId { get; private set; }
    public TenantType Type { get; private set; }
    public TenantStatus Status { get; private set; }
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string? Branding { get; private set; }
    public string? Settings { get; private set; }

    public IReadOnlyCollection<TenantFeature> Features => _features.AsReadOnly();

    public static Result<Tenant> Create(
        string slug,
        string name,
        TenantType type,
        string address,
        string city,
        string country,
        string? legalId = null)
    {
        if (string.IsNullOrWhiteSpace(slug) || !SlugRegex().IsMatch(slug))
        {
            return Error.Validation(
                "Tenant.InvalidSlug",
                "Slug must be lowercase kebab-case (letters, digits and hyphens only).");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Tenant.NameRequired", "Tenant name is required.");
        }

        var tenant = new Tenant(
            Guid.NewGuid(),
            slug,
            name.Trim(),
            type,
            address?.Trim() ?? string.Empty,
            city?.Trim() ?? string.Empty,
            country?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(legalId) ? null : legalId.Trim());

        tenant.Raise(new TenantCreatedDomainEvent(tenant.Id, tenant.Slug));
        return tenant;
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Tenant.NameRequired", "Tenant name is required.");
        }

        Name = name.Trim();
        return Result.Success();
    }

    public Result UpdateProfile(string? legalId, string address, string city, string country)
    {
        LegalId = string.IsNullOrWhiteSpace(legalId) ? null : legalId.Trim();
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        Country = country?.Trim() ?? string.Empty;
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == TenantStatus.Active)
        {
            return Error.Conflict("Tenant.AlreadyActive", "The tenant is already active.");
        }

        Status = TenantStatus.Active;
        Raise(new TenantStatusChangedDomainEvent(Id, Slug, Status.ToString()));
        return Result.Success();
    }

    public Result Suspend()
    {
        if (Status == TenantStatus.Suspended)
        {
            return Error.Conflict("Tenant.AlreadySuspended", "The tenant is already suspended.");
        }

        Status = TenantStatus.Suspended;
        Raise(new TenantStatusChangedDomainEvent(Id, Slug, Status.ToString()));
        return Result.Success();
    }

    public Result ReturnToOnboarding()
    {
        if (Status == TenantStatus.Onboarding)
        {
            return Error.Conflict("Tenant.AlreadyOnboarding", "The tenant is already in onboarding.");
        }

        Status = TenantStatus.Onboarding;
        Raise(new TenantStatusChangedDomainEvent(Id, Slug, Status.ToString()));
        return Result.Success();
    }

    public void SetBranding(string? branding) => Branding = branding;

    public void SetSettings(string? settings) => Settings = settings;

    // Idempotent upsert: flips an existing feature row or adds one. Enabling/disabling twice is a no-op.
    public void SetFeature(FeatureKey key, bool enabled)
    {
        var feature = _features.FirstOrDefault(f => f.FeatureKey == key);
        if (feature is null)
        {
            _features.Add(new TenantFeature(Guid.NewGuid(), Id, key, enabled));
            return;
        }

        feature.SetEnabled(enabled);
    }

    public void EnableFeature(FeatureKey key) => SetFeature(key, enabled: true);

    public void DisableFeature(FeatureKey key) => SetFeature(key, enabled: false);

    public bool IsFeatureEnabled(FeatureKey key) =>
        _features.Any(f => f.FeatureKey == key && f.Enabled);

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SlugRegex();
}
