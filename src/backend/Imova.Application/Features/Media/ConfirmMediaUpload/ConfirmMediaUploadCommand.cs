using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

// RequestingUserId/IsAdmin always come from the caller's JWT (see MediaAccess for who may upload).
public record ConfirmMediaUploadCommand(Guid ListingId, string BlobName, Guid RequestingUserId, bool IsAdmin)
    : IRequest<PhotoDto>;
