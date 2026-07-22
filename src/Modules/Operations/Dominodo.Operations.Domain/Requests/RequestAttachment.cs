namespace Dominodo.Operations.Domain.Requests;

// Evidence (photos/files) hanging off a request or a specific update (domain-model §3.1.2). Child
// entity under the Request aggregate — only mutated through Request.
public sealed class RequestAttachment
{
    private RequestAttachment() { } // EF Core

    internal RequestAttachment(
        Guid id,
        Guid requestId,
        Guid? requestUpdateId,
        string fileUrl,
        string fileName,
        string contentType,
        Guid uploadedByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        RequestId = requestId;
        RequestUpdateId = requestUpdateId;
        FileUrl = fileUrl;
        FileName = fileName;
        ContentType = contentType;
        UploadedByUserId = uploadedByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid? RequestUpdateId { get; private set; }
    public string FileUrl { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
