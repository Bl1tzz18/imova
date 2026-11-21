namespace Imova.Application.Features.Messaging;

public enum MessageImageSize
{
    // The file as the sender uploaded it.
    Original,

    // The thumbnail in a conversation (shown at about 192×144; 800 px covers phone screens at 3×).
    Thumbnail,

    // The full-screen viewer.
    Large,
}

// The display copies of a message image. The original stays untouched (it's what the personal-data
// export contains); next to it in the private container, under derived names, live JPEGs no larger
// than these longest sides: "messages/{userId}/{guid}.png" → "…/{guid}_800.jpg", "…_1600.jpg".
// Unlike listing photos they're made the first time someone asks for them (MessageImageCopies), so
// images sent before they existed get them too, with nothing to backfill.
public static class MessageImageSizes
{
    public static readonly IReadOnlyList<MessageImageSize> Copies = [MessageImageSize.Thumbnail, MessageImageSize.Large];

    public static int LongestSide(MessageImageSize size) => size switch
    {
        MessageImageSize.Thumbnail => 800,
        MessageImageSize.Large => 1600,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static string BlobName(string originalBlobName, MessageImageSize size)
    {
        if (size == MessageImageSize.Original)
        {
            return originalBlobName;
        }

        var extension = Path.GetExtension(originalBlobName);
        return $"{originalBlobName[..^extension.Length]}_{LongestSide(size)}.jpg";
    }

    // The original and its copies — what has to go (or stay) together. Copies are listed even if
    // they were never made: deleting a missing file is a no-op.
    public static IEnumerable<string> AllBlobNames(string originalBlobName) =>
        [originalBlobName, .. Copies.Select(size => BlobName(originalBlobName, size))];

    // The API's ?size= value: absent → the original, "thumbnail" / "large" (any case) → that copy;
    // anything else isn't a size.
    public static bool TryParse(string? value, out MessageImageSize size)
    {
        size = MessageImageSize.Original;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (string.Equals(value, "thumbnail", StringComparison.OrdinalIgnoreCase))
        {
            size = MessageImageSize.Thumbnail;
            return true;
        }

        if (string.Equals(value, "large", StringComparison.OrdinalIgnoreCase))
        {
            size = MessageImageSize.Large;
            return true;
        }

        return false;
    }
}
