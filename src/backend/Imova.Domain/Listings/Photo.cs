using Imova.Domain.Common;

namespace Imova.Domain.Listings;

// One uploaded image for a Listing. Rows are created once the blob has actually landed in
// storage (see ConfirmMediaUpload) and start out Pending until moderated.
//
// ListingId is *not* a foreign key (see PhotoConfiguration) — the add-listing form lets a client
// start attaching photos under a client-generated listing id before the Listing row itself exists
// (CreateListing accepts that same id later). Abandoned drafts currently leak orphaned photo
// rows/blobs — no cleanup job yet.
//
// Stores the blob *name*, not a full URL: the public URL depends on the storage account/host
// (Azurite locally vs. real Azure), so it's derived at read time via IBlobStorageService.
public sealed class Photo : Entity
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

    private Photo(
        Guid id,
        Guid listingId,
        string blobName,
        string contentType,
        long fileSizeBytes,
        int sortOrder,
        bool isPrimary)
        : base(id)
    {
        ListingId = listingId;
        BlobName = blobName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        ModerationStatus = ModerationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ListingId { get; private set; }

    public string BlobName { get; private set; }

    public string ContentType { get; private set; }

    public long FileSizeBytes { get; private set; }

    public int SortOrder { get; private set; }

    // The cover image shown on cards/search results. At most one per listing — maintained by the
    // Application layer (first upload becomes primary; deleting it promotes the next one).
    public bool IsPrimary { get; private set; }

    public ModerationStatus ModerationStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ModeratedAt { get; private set; }

    public static Photo Create(
        Guid listingId,
        string blobName,
        string contentType,
        long fileSizeBytes,
        int sortOrder = 0,
        bool isPrimary = false)
    {
        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("ListingId is required.", nameof(listingId));
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

        return new Photo(Guid.NewGuid(), listingId, blobName, contentType, fileSizeBytes, sortOrder, isPrimary);
    }

    public void MarkAsPrimary() => IsPrimary = true;

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
