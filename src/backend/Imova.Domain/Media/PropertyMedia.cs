using Imova.Domain.Common;

namespace Imova.Domain.Media;

// One uploaded image for a property listing. Rows are created once the blob has actually
// landed in storage (see ConfirmMediaUpload) and start out Pending until moderated.
//
// PropertyId is *not* a foreign key (see PropertyMediaConfiguration) — the upload flow lets a
// client start attaching photos under a client-generated property id before the Property row
// itself exists (CreateProperty accepts that same id later), the same "trust the client for an
// id, no auth yet" stopgap already used for Property.OwnerId. Revisit once listing creation is a
// proper multi-step flow (create draft -> attach media -> publish) or auth exists to scope this
// more tightly. Abandoned drafts currently leak orphaned media rows/blobs — no cleanup job yet.
public sealed class PropertyMedia : Entity
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".heic"] = "image/heic",
            [".heif"] = "image/heif",
        };

    private PropertyMedia(
        Guid id,
        Guid propertyId,
        string blobName,
        string contentType,
        long fileSizeBytes,
        int sortOrder)
        : base(id)
    {
        PropertyId = propertyId;
        BlobName = blobName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        SortOrder = sortOrder;
        ModerationStatus = ModerationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid PropertyId { get; private set; }

    public string BlobName { get; private set; }

    public string ContentType { get; private set; }

    public long FileSizeBytes { get; private set; }

    public int SortOrder { get; private set; }

    public ModerationStatus ModerationStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ModeratedAt { get; private set; }

    public static PropertyMedia Create(
        Guid propertyId,
        string blobName,
        string contentType,
        long fileSizeBytes,
        int sortOrder = 0)
    {
        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("PropertyId is required.", nameof(propertyId));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("BlobName is required.", nameof(blobName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("ContentType is required.", nameof(contentType));
        }

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "FileSizeBytes must be greater than zero.");
        }

        if (fileSizeBytes > MaxFileSizeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), $"FileSizeBytes exceeds the {MaxFileSizeBytes} byte limit.");
        }

        return new PropertyMedia(Guid.NewGuid(), propertyId, blobName, contentType, fileSizeBytes, sortOrder);
    }

    // TODO: no moderation pipeline yet (manual review queue or an automated image-safety check).
    // These just flip the status once one exists; nothing currently calls them.
    public void Approve()
    {
        ModerationStatus = ModerationStatus.Approved;
        ModeratedAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        ModerationStatus = ModerationStatus.Rejected;
        ModeratedAt = DateTimeOffset.UtcNow;
    }
}
