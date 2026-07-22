using System.Text.Json;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Announcements;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Operations.Application.Announcements.GetMyAnnouncements;

// Member-facing feed (auth-only, NO permission): active announcements for the current tenant, narrowed to
// the caller's audience — AllTenant always; ByTower/ByApartments matched against the caller's apartments/
// towers (resolved via the Tenants facade) — with an optional Category filter. Ordered by Priority asc
// (0 = highest) then PublishedAtUtc desc (domain-model §3.4).
internal sealed record GetMyAnnouncementsQuery(string? Category = null) : IQuery<IReadOnlyList<AnnouncementDto>>;

internal sealed class GetMyAnnouncementsQueryHandler(
    IAnnouncementRepository announcements,
    ICurrentUser currentUser,
    ITenantsModuleApi tenantsModule,
    IClock clock)
    : IQueryHandler<GetMyAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    public async Task<Result<IReadOnlyList<AnnouncementDto>>> Handle(GetMyAnnouncementsQuery query, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var active = await announcements.ListActiveAsync(query.Category, now, ct);

        var myApartments = await tenantsModule.GetApartmentsForResidentAsync(currentUser.UserId, ct);
        var myApartmentIds = myApartments.Select(a => a.ApartmentId).ToHashSet();
        var myTowers = myApartments
            .Where(a => !string.IsNullOrWhiteSpace(a.Tower))
            .Select(a => a.Tower!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var visible = active
            .Where(a => IsInAudience(a, myApartmentIds, myTowers))
            .OrderBy(a => a.Priority)
            .ThenByDescending(a => a.PublishedAtUtc)
            .Select(AnnouncementMappers.ToDto)
            .ToList();

        return visible;
    }

    private static bool IsInAudience(Announcement a, HashSet<Guid> apartmentIds, HashSet<string> towers) =>
        a.AudienceType switch
        {
            AudienceType.AllTenant => true,
            AudienceType.ByTower => ParseStrings(a.AudienceFilter).Any(towers.Contains),
            AudienceType.ByApartments => ParseGuids(a.AudienceFilter).Any(apartmentIds.Contains),
            _ => false,
        };

    private static IEnumerable<string> ParseStrings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<Guid> ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
