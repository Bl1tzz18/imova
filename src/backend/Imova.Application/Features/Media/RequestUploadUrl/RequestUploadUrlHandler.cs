using Imova.Application.Common.Interfaces;
using Imova.Contracts.Media;
using MediatR;

namespace Imova.Application.Features.Media.RequestUploadUrl;

public class RequestUploadUrlHandler(IBlobStorageService blobStorageService)
    : IRequestHandler<RequestUploadUrlCommand, UploadUrlDto>
{
    public Task<UploadUrlDto> Handle(RequestUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var expiry = blobStorageService.DefaultUploadExpiry;
        var blobName = blobStorageService.GenerateBlobName(request.PropertyId, request.FileExtension);
        var uploadUrl = blobStorageService.GenerateUploadSasUrl(blobName, expiry);

        return Task.FromResult(new UploadUrlDto(uploadUrl, blobName, DateTimeOffset.UtcNow.Add(expiry)));
    }
}
