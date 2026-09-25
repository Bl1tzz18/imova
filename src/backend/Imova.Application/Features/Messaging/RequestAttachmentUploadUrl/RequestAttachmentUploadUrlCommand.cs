using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.RequestAttachmentUploadUrl;

// Step 1 of attaching an image: a SAS URL the browser PUTs the file to. The returned blob name is
// then passed with the message (see MessageAttachments for the checks that happen at send time).
public record RequestAttachmentUploadUrlCommand(Guid UserId, string FileExtension) : IRequest<AttachmentUploadUrlDto>;
