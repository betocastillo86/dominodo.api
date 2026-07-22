namespace Dominodo.Operations.Domain.Announcements;

// Lifecycle (domain-model §3.4): Draft → Published → Archived.
public enum AnnouncementStatus
{
    Draft,
    Published,
    Archived
}
