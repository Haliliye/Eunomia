namespace TodoApp.Domain.UserStories;

/// <summary>
/// File attachment metadata on a UserStory (US-134/135/136) — modeled like
/// ChecklistItem, as part of the aggregate rather than its own aggregate root.
/// The actual file bytes live outside Mongo entirely (see IAttachmentStorage);
/// this only tracks what's needed to find and describe the file.
/// </summary>
public class Attachment
{
    public string Id { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string UploadedByUserId { get; private set; } = string.Empty;
    public DateTime UploadedOn { get; private set; }

    private Attachment() { }

    public Attachment(string id, string fileName, string contentType, long sizeBytes, string storageKey, string uploadedByUserId, DateTime uploadedOn)
    {
        Id = id;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        UploadedOn = uploadedOn;
    }
}
