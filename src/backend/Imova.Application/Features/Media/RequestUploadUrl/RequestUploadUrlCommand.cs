using Imova.Contracts.Media;
using MediatR;

namespace Imova.Application.Features.Media.RequestUploadUrl;

// FileExtension includes the leading dot (".jpg") — see PropertyMedia.AllowedContentTypesByExtension.
public record RequestUploadUrlCommand(Guid PropertyId, string FileExtension) : IRequest<UploadUrlDto>;
