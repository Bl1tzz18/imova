using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Messaging;

public sealed record MessageImageContent(Stream Content, string ContentType);

// Serves a message image in the size asked for (see MessageImageSizes). A copy that doesn't exist
// yet is made on the spot — both copies from one decode of the original — stored for next time and
// returned. If the original can't be resized (or storing the copies fails), the original is served
// as before: a slower image, never a broken one. Who may see the image is decided before this
// (GetMessageAttachment); this only deals with files.
public sealed class MessageImageCopies(
    IBlobStorageService blobStorageService,
    IPhotoResizer photoResizer,
    ILogger<MessageImageCopies> logger)
{
    // Null when the original is gone.
    public async Task<MessageImageContent?> OpenAsync(
        string originalBlobName, string originalContentType, MessageImageSize size, CancellationToken cancellationToken)
    {
        if (size != MessageImageSize.Original)
        {
            var copy = await blobStorageService.OpenMessageAttachmentAsync(MessageImageSizes.BlobName(originalBlobName, size), cancellationToken);
            if (copy is not null)
            {
                return new MessageImageContent(copy, "image/jpeg");
            }

            var made = await MakeCopiesAsync(originalBlobName, cancellationToken);
            if (made is not null)
            {
                return new MessageImageContent(new MemoryStream(made[size]), "image/jpeg");
            }
        }

        var original = await blobStorageService.OpenMessageAttachmentAsync(originalBlobName, cancellationToken);
        return original is null ? null : new MessageImageContent(original, originalContentType);
    }

    private async Task<Dictionary<MessageImageSize, byte[]>?> MakeCopiesAsync(string originalBlobName, CancellationToken cancellationToken)
    {
        IReadOnlyList<byte[]>? jpegs;
        try
        {
            await using var source = await blobStorageService.OpenMessageAttachmentAsync(originalBlobName, cancellationToken);
            if (source is null)
            {
                return null;
            }

            jpegs = await photoResizer.ResizeToJpegAsync(
                source, MessageImageSizes.Copies.Select(MessageImageSizes.LongestSide).ToList(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not resize message image {BlobName}; serving the original.", originalBlobName);
            return null;
        }

        if (jpegs is null)
        {
            logger.LogWarning("Message image {BlobName} couldn't be read as an image; serving the original.", originalBlobName);
            return null;
        }

        var made = MessageImageSizes.Copies.Zip(jpegs).ToDictionary(pair => pair.First, pair => pair.Second);
        foreach (var (size, bytes) in made)
        {
            try
            {
                using var content = new MemoryStream(bytes);
                await blobStorageService.UploadMessageAttachmentAsync(
                    MessageImageSizes.BlobName(originalBlobName, size), content, "image/jpeg", cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Served anyway; the next request tries to store it again.
                logger.LogWarning(ex, "Could not store a copy of message image {BlobName}.", originalBlobName);
            }
        }

        return made;
    }
}
