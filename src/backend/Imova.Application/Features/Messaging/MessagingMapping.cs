using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Messaging;

namespace Imova.Application.Features.Messaging;

public static class MessagingMapping
{
    public static MessageDto ToDto(this Message message, IBlobStorageService blobStorageService) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.Body,
            message.Attachments
                .OrderBy(a => a.SortOrder)
                .Select(a => new MessageAttachmentDto(a.Id, blobStorageService.GetPublicUrl(a.BlobName), a.ContentType))
                .ToList(),
            message.CreatedAt,
            message.Status.ToString());
}
