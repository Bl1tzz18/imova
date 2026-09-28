using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;

namespace Imova.Application.Features.Messaging;

// Turns the blob names a sender's browser uploaded (via RequestAttachmentUploadUrl) into verified
// attachments — same checks as listing photos: the file exists, is under the size limit and really
// is an image. Only the sender's own "messages/{senderUserId}/" uploads are accepted.
public static class MessageAttachments
{
    public static async Task<List<MessageAttachment>> ResolveAsync(
        IBlobStorageService blobStorageService,
        Guid senderUserId,
        IReadOnlyList<string>? blobNames,
        CancellationToken cancellationToken)
    {
        var names = (blobNames ?? []).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        if (names.Count > Message.MaxAttachments)
        {
            throw Invalid($"A message can have at most {Message.MaxAttachments} images.", ErrorCodes.TooManyImages, CodedFailure.Params(("max", Message.MaxAttachments)));
        }

        var ownPrefix = $"messages/{senderUserId}/";
        var attachments = new List<MessageAttachment>();
        foreach (var name in names)
        {
            if (!name.StartsWith(ownPrefix, StringComparison.Ordinal) || name.Contains(".."))
            {
                throw Invalid("An attachment doesn't belong to you.", ErrorCodes.UploadNotYours);
            }

            var info = await blobStorageService.TryGetMessageAttachmentInfoAsync(name, cancellationToken)
                ?? throw Invalid("An image was not found in storage — the upload may not have completed.", ErrorCodes.UploadNotFound);

            if (info.SizeBytes > Photo.MaxFileSizeBytes)
            {
                throw Invalid($"An image exceeds the {Photo.MaxFileSizeBytes / (1024 * 1024)}MB limit.", ErrorCodes.UploadTooLarge, CodedFailure.Params(("maxMb", Photo.MaxFileSizeBytes / (1024 * 1024))));
            }

            var contentType = ImageSignature.DetectContentType(info.LeadingBytes)
                ?? throw Invalid("An attachment is not a recognized image format (JPEG, PNG, WebP).", ErrorCodes.UploadNotAnImage);

            attachments.Add(new MessageAttachment(name, contentType, info.SizeBytes));
        }

        return attachments;
    }

    private static ValidationException Invalid(string message, string code, IReadOnlyDictionary<string, object>? parameters = null) =>
        new([CodedFailure.Of("AttachmentBlobNames", message, code, parameters)]);
}
