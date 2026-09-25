using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.RequestAttachmentUploadUrl;

public class RequestAttachmentUploadUrlHandler(IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<RequestAttachmentUploadUrlCommand, AttachmentUploadUrlDto>
{
    public Task<AttachmentUploadUrlDto> Handle(RequestAttachmentUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var blobName = blobStorageService.GenerateMessageAttachmentBlobName(request.UserId, request.FileExtension);
        var expiry = blobStorageService.DefaultUploadExpiry;
        return Task.FromResult(new AttachmentUploadUrlDto(
            blobStorageService.GenerateMessageAttachmentUploadSasUrl(blobName, expiry), blobName, timeProvider.GetUtcNow().Add(expiry)));
    }
}
