using System.Text.Json;
using Dominodo.E2E.Clients.Common;
using Dominodo.E2E.Clients.Modules.Operations.Models;
using Dominodo.E2E.Clients.Modules.Tenants;
using Dominodo.E2E.Core;
using Dominodo.E2E.Core.Security;

namespace Dominodo.E2E.Clients.Modules.Operations;

/// <summary>
/// Builds Operations-module (Announcements) request models (fake but valid data by default) and composes
/// full <c>Arrange</c> use cases. Per README §8, any Arrange helper that calls the API throws on
/// non-success — a broken Arrange aborts the test rather than producing a misleading Assert. Composite
/// helpers take every dependency as an optional parameter and create only what the caller did not supply
/// (see the skill's "self-completing Arrange" tip): a fresh tenant is created when <c>tenantSlug</c> is
/// omitted. All writes use Platform announcements.* tokens (cross-tenant grants), so a helper works for
/// any resolved tenant. Cross-module residency Arrange (for the <c>/mine</c> feed) is delegated to
/// <see cref="TenantsRequestBuilder"/>.
/// </summary>
public sealed class OperationsRequestBuilder(
    IOperationsClient operations,
    TenantsRequestBuilder tenants,
    JwtTokenFactory jwtTokenFactory)
    : BaseRequestBuilder
{
    private readonly IOperationsClient _operations = operations;
    private readonly TenantsRequestBuilder _tenants = tenants;
    private readonly JwtTokenFactory _jwtTokenFactory = jwtTokenFactory;

    /// <summary>Serializes a ByTower audience filter (a JSON <c>string[]</c> of tower names).</summary>
    public static string TowerFilter(params string[] towers) => JsonSerializer.Serialize(towers);

    /// <summary>Serializes a ByApartments audience filter (a JSON <c>Guid[]</c> of apartment ids).</summary>
    public static string ApartmentsFilter(params Guid[] apartmentIds) => JsonSerializer.Serialize(apartmentIds);

    /// <summary>
    /// Builds a valid <see cref="NewAnnouncementModel"/> (default AllTenant, priority 5). Any field is
    /// overridable: <c>model with { AudienceType = "ByTower", AudienceFilter = TowerFilter("A") }</c>.
    /// Does NOT call the API.
    /// </summary>
    public NewAnnouncementModel BuildNewAnnouncementModel(
        string? title = null,
        string? body = null,
        byte priority = 5,
        string audienceType = "AllTenant",
        string? audienceFilter = null,
        string? category = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        return new NewAnnouncementModel
        {
            Title = title ?? $"Comunicado {Faker.Lorem.Sentence(3)}",
            Body = body ?? Faker.Lorem.Paragraph(),
            Priority = priority,
            AudienceType = audienceType,
            AudienceFilter = audienceFilter,
            Category = category,
            ExpiresAtUtc = expiresAtUtc,
        };
    }

    /// <summary>
    /// Builds a valid <see cref="UpdateAnnouncementModel"/> (default AllTenant, priority 5). Any field is
    /// overridable. Does NOT call the API.
    /// </summary>
    public UpdateAnnouncementModel BuildUpdateAnnouncementModel(
        string? title = null,
        string? body = null,
        byte priority = 5,
        string audienceType = "AllTenant",
        string? audienceFilter = null,
        string? category = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        return new UpdateAnnouncementModel
        {
            Title = title ?? $"Comunicado {Faker.Lorem.Sentence(3)}",
            Body = body ?? Faker.Lorem.Paragraph(),
            Priority = priority,
            AudienceType = audienceType,
            AudienceFilter = audienceFilter,
            Category = category,
            ExpiresAtUtc = expiresAtUtc,
        };
    }

    /// <summary>
    /// Self-completing Arrange (parameter overload): builds a draft announcement, creating its
    /// <b>tenant too</b> when <paramref name="tenantSlug"/> is omitted. Convenience over
    /// <see cref="BuildNewAnnouncementModel"/> + <see cref="CreateAnnouncementAsync(NewAnnouncementModel, string)"/>.
    /// </summary>
    public async Task<CreatedAnnouncement> CreateAnnouncementAsync(
        string? tenantSlug = null,
        string? title = null,
        string? body = null,
        byte priority = 5,
        string audienceType = "AllTenant",
        string? audienceFilter = null,
        string? category = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        tenantSlug ??= (await _tenants.CreateTenantAsync()).Slug;
        var model = BuildNewAnnouncementModel(
            title, body, priority, audienceType, audienceFilter, category, expiresAtUtc);
        return await CreateAnnouncementAsync(model, tenantSlug);
    }

    /// <summary>
    /// Full Arrange: creates the draft with a Platform <c>announcements.create</c> token (cross-tenant, so
    /// it works for any resolved tenant), scoped by <paramref name="tenantSlug"/>, reads it back via
    /// <c>GET /announcements/{id}</c> and returns the persisted model. Throws on any non-success step.
    /// </summary>
    public async Task<CreatedAnnouncement> CreateAnnouncementAsync(NewAnnouncementModel model, string tenantSlug)
    {
        var createToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsCreate);

        var response = await _operations.CreateAnnouncement(model, tenantSlug, createToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: creating an announcement in tenant '{tenantSlug}' returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }

        var announcement = await GetAnnouncementAsync(tenantSlug, response.Content!.Id);
        return new CreatedAnnouncement(tenantSlug, announcement);
    }

    /// <summary>
    /// Self-completing Arrange: a draft created (with its tenant, when omitted) and then <b>published</b>,
    /// so it is an ACTIVE announcement — the precondition the <c>/mine</c> feed reads. Returns the persisted
    /// (Published) model. Throws on any non-success step.
    /// </summary>
    public async Task<CreatedAnnouncement> CreatePublishedAnnouncementAsync(
        string? tenantSlug = null,
        string? title = null,
        string? body = null,
        byte priority = 5,
        string audienceType = "AllTenant",
        string? audienceFilter = null,
        string? category = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        var draft = await CreateAnnouncementAsync(
            tenantSlug, title, body, priority, audienceType, audienceFilter, category, expiresAtUtc);

        await PublishAnnouncementAsync(draft.TenantSlug, draft.Id);
        var published = await GetAnnouncementAsync(draft.TenantSlug, draft.Id);
        return new CreatedAnnouncement(draft.TenantSlug, published);
    }

    /// <summary>
    /// Full Arrange: publishes an announcement with a Platform <c>announcements.edit</c> token, scoped by
    /// <paramref name="tenantSlug"/>. Throws on non-success so a broken Arrange aborts the test.
    /// </summary>
    public async Task PublishAnnouncementAsync(string tenantSlug, Guid id)
    {
        var editToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        var response = await _operations.PublishAnnouncement(id, tenantSlug, editToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: publishing announcement {id} in tenant '{tenantSlug}' returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }
    }

    /// <summary>
    /// Reads an announcement back via <c>GET /announcements/{id}</c> (Platform <c>announcements.view</c>
    /// token, scoped by <paramref name="tenantSlug"/>) and returns the persisted model. Throws on non-success.
    /// </summary>
    public async Task<AnnouncementDetailModel> GetAnnouncementAsync(string tenantSlug, Guid id)
    {
        var viewToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        var response = await _operations.GetAnnouncementById(id, tenantSlug, viewToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: GET announcement {id} in tenant '{tenantSlug}' returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }

        return response.Content!;
    }
}
