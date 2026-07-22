using Dominodo.Operations.Domain.Announcements.Events;
using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Announcements;

// A tenant announcement / bulletin (domain-model §3.4), an ITenantOwned aggregate. Admins draft, publish
// and archive; members read the active ones via /mine. Category is a free-form, tenant-defined column
// (filterable). AudienceFilter is raw JSON (tower strings / apartment Guids from Tenants) interpreted by
// the reading side. State changes only through the methods below.
public sealed class Announcement : AggregateRoot, ITenantOwned
{
    private readonly List<AnnouncementAttachment> _attachments = new();

    private Announcement() { } // EF Core

    private Announcement(
        Guid id,
        Guid tenantId,
        string title,
        string body,
        byte priority,
        AudienceType audienceType,
        string? audienceFilter,
        string? category,
        DateTimeOffset? expiresAtUtc) : base(id)
    {
        TenantId = tenantId;
        Title = title;
        Body = body;
        Priority = priority;
        AudienceType = audienceType;
        AudienceFilter = audienceFilter;
        Category = category;
        ExpiresAtUtc = expiresAtUtc;
        Status = AnnouncementStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string? Category { get; private set; }
    public byte Priority { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public AudienceType AudienceType { get; private set; }
    public string? AudienceFilter { get; private set; }
    public AnnouncementStatus Status { get; private set; }
    public Guid? PublishedByUserId { get; private set; }

    public IReadOnlyCollection<AnnouncementAttachment> Attachments => _attachments.AsReadOnly();

    public static Result<Announcement> CreateDraft(
        Guid tenantId,
        string title,
        string body,
        byte priority,
        AudienceType audienceType,
        string? audienceFilter,
        string? category,
        DateTimeOffset? expiresAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("Announcement.TenantRequired", "A tenant is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("Announcement.TitleRequired", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Error.Validation("Announcement.BodyRequired", "Body is required.");
        }

        return new Announcement(
            Guid.NewGuid(),
            tenantId,
            title.Trim(),
            body.Trim(),
            priority,
            audienceType,
            NormalizeFilter(audienceFilter),
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            expiresAtUtc);
    }

    public Result Update(
        string title,
        string body,
        byte priority,
        AudienceType audienceType,
        string? audienceFilter,
        string? category,
        DateTimeOffset? expiresAtUtc)
    {
        if (Status == AnnouncementStatus.Archived)
        {
            return Error.Conflict("Announcement.Archived", "An archived announcement cannot be edited.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("Announcement.TitleRequired", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Error.Validation("Announcement.BodyRequired", "Body is required.");
        }

        Title = title.Trim();
        Body = body.Trim();
        Priority = priority;
        AudienceType = audienceType;
        AudienceFilter = NormalizeFilter(audienceFilter);
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        ExpiresAtUtc = expiresAtUtc;
        return Result.Success();
    }

    public Result Publish(Guid publishedByUserId, DateTimeOffset nowUtc)
    {
        if (Status != AnnouncementStatus.Draft)
        {
            return Error.Conflict("Announcement.NotDraft", "Only a draft announcement can be published.");
        }

        Status = AnnouncementStatus.Published;
        PublishedAtUtc = nowUtc;
        PublishedByUserId = publishedByUserId;

        Raise(new AnnouncementPublishedDomainEvent(Id, TenantId, publishedByUserId, nowUtc));
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status == AnnouncementStatus.Archived)
        {
            return Error.Conflict("Announcement.AlreadyArchived", "The announcement is already archived.");
        }

        Status = AnnouncementStatus.Archived;
        return Result.Success();
    }

    // "Active" (domain-model §3.4): published and not past its expiry.
    public bool IsActive(DateTimeOffset nowUtc) =>
        Status == AnnouncementStatus.Published && (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc);

    private static string? NormalizeFilter(string? audienceFilter) =>
        string.IsNullOrWhiteSpace(audienceFilter) ? null : audienceFilter.Trim();
}
