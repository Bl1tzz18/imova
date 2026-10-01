using Imova.Domain.Common;

namespace Imova.Domain.Listings;

// One uploaded image for a Listing. Rows are created once the blob has actually landed in
// storage (see ConfirmMediaUpload) and start out Pending until moderated.
//
// ListingId is *not* a foreign key (see PhotoConfiguration) — the add-listing form lets a client
// start attaching photos under a client-generated listing id before the Listing row itself exists
// (CreateListing accepts that same id later). UploadedByUserId is what ties such photos to the
// person who will create that listing (see MediaAccess). Photos whose listing never got created
// are deleted after a week by the Worker (AbandonedPhotoCleanup).
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
        bool isPrimary,
        Guid? uploadedByUserId)
        : base(id)
    {
        ListingId = listingId;
        UploadedByUserId = uploadedByUserId;
        BlobName = blobName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        ModerationStatus = ModerationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ListingId { get; private set; }

    // Who uploaded it. Null only for photos from before this was recorded.
    public Guid? UploadedByUserId { get; private set; }

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

    // Which generation of the smaller display copies (thumbnail/card/large JPEGs next to the
    // original, see PhotoSizes in Application) exists for this photo; 0 = none yet, and until
    // then the original is shown. Set when the photo is confirmed, or by the Worker's backfill.
    public int SizesVersion { get; private set; }

    // When making the copies failed for good (the original is gone, or can't be read as an
    // image) — the backfill doesn't try again.
    public DateTimeOffset? SizesFailedAt { get; private set; }

    public static Photo Create(
        Guid listingId,
        string blobName,
        string contentType,
        long fileSizeBytes,
        int sortOrder = 0,
        bool isPrimary = false,
        Guid? uploadedByUserId = null)
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

        return new Photo(Guid.NewGuid(), listingId, blobName, contentType, fileSizeBytes, sortOrder, isPrimary, uploadedByUserId);
    }

    public void MarkAsPrimary() => IsPrimary = true;

    public void MarkSizesGenerated(int version)
    {
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "A sizes version starts at 1.");
        }

        SizesVersion = version;
        SizesFailedAt = null;
    }

    public void MarkSizesFailed(DateTimeOffset at) => SizesFailedAt = at;

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
