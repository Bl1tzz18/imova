using Imova.Contracts.Media;
using MediatR;

namespace Imova.Application.Features.Media.RequestUploadUrl;

// FileExtension includes the leading dot (".jpg") — see Photo.AllowedContentTypesByExtension.
public record RequestUploadUrlCommand(Guid ListingId, string FileExtension) : IRequest<UploadUrlDto>;
