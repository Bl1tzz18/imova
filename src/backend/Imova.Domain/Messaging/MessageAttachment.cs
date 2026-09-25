namespace Imova.Domain.Messaging;

// An image sent with a Message — uploaded straight to blob storage like listing photos, and
// stored by blob *name* (the URL is derived at read time, same as Photo).
public sealed class MessageAttachment
{
    // For EF Core materialization only.
    private MessageAttachment()
    {
        BlobName = null!;
        ContentType = null!;
    }

    public MessageAttachment(string blobName, string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("BlobName is required.", nameof(blobName));
        }

        Id = Guid.NewGuid();
        BlobName = blobName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string BlobName { get; private set; }

    public string ContentType { get; private set; }

    public long SizeBytes { get; private set; }

    public int SortOrder { get; private set; }

    internal MessageAttachment For(Guid messageId, int sortOrder)
    {
        MessageId = messageId;
        SortOrder = sortOrder;
        return this;
    }
}
