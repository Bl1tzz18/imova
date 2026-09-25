using Imova.Contracts.Messaging;
using Imova.Domain.Messaging;

namespace Imova.Application.Features.Messaging;

public static class MessagingMapping
{
    // Where participants (and admins) fetch an attachment — an access-checked API route, never a
    // storage URL (the images live in a private container).
    public static string AttachmentPath(Guid attachmentId) => $"/api/v1/messaging/attachments/{attachmentId}";

    public static MessageDto ToDto(this Message message) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.Body,
            message.Attachments
                .OrderBy(a => a.SortOrder)
                .Select(a => new MessageAttachmentDto(a.Id, AttachmentPath(a.Id), a.ContentType))
                .ToList(),
            message.CreatedAt,
            message.Status.ToString());
}
