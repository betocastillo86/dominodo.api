namespace Dominodo.Operations.Domain.Announcements;

// Attachment on an announcement (domain-model §3.4 — same shape as RequestAttachment). Child entity
// under the Announcement aggregate — only mutated through Announcement.
public sealed class AnnouncementAttachment
{
    private AnnouncementAttachment() { } // EF Core

    internal AnnouncementAttachment(
        Guid id,
        Guid announcementId,
        string fileUrl,
        string fileName,
        string contentType,
        Guid uploadedByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        AnnouncementId = announcementId;
        FileUrl = fileUrl;
        FileName = fileName;
        ContentType = contentType;
        UploadedByUserId = uploadedByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid AnnouncementId { get; private set; }
    public string FileUrl { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
