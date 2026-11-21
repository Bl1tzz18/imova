using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Media;
using MediatR;

namespace Imova.Application.Features.Media.RequestUploadUrl;

public class RequestUploadUrlHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<RequestUploadUrlCommand, UploadUrlDto>
{
    public async Task<UploadUrlDto> Handle(RequestUploadUrlCommand request, CancellationToken cancellationToken)
    {
        await MediaAccess.EnsureCanUploadAsync(dbContext, request.ListingId, request.RequestingUserId, request.IsAdmin, cancellationToken);
        await ListingPhotoRules.EnsureRoomForAnotherAsync(dbContext, request.ListingId, nameof(RequestUploadUrlCommand.ListingId), cancellationToken);

        var expiry = blobStorageService.DefaultUploadExpiry;
        var blobName = blobStorageService.GenerateBlobName(request.ListingId, request.FileExtension);
        var uploadUrl = blobStorageService.GenerateUploadSasUrl(blobName, expiry);

        return new UploadUrlDto(uploadUrl, blobName, DateTimeOffset.UtcNow.Add(expiry));
    }
}
