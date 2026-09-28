using Imova.Contracts.Media;
using MediatR;

namespace Imova.Application.Features.Media.RequestUploadUrl;

// FileExtension includes the leading dot (".jpg") — see Photo.AllowedContentTypesByExtension.
// RequestingUserId/IsAdmin always come from the caller's JWT (see MediaAccess for who may upload).
public record RequestUploadUrlCommand(Guid ListingId, string FileExtension, Guid RequestingUserId, bool IsAdmin)
    : IRequest<UploadUrlDto>;
