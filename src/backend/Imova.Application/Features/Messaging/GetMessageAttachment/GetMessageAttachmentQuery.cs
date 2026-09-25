using MediatR;

namespace Imova.Application.Features.Messaging.GetMessageAttachment;

public record MessageAttachmentFile(string BlobName, string ContentType);

// An image sent in a conversation — only for its two participants, or an admin. Null otherwise
// (and for an unknown id), so it can't be used to probe which attachments exist.
public record GetMessageAttachmentQuery(Guid UserId, bool IsAdmin, Guid AttachmentId) : IRequest<MessageAttachmentFile?>;
