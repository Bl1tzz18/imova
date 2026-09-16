using Imova.Contracts.Media;
using MediatR;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

public record ConfirmMediaUploadCommand(Guid PropertyId, string BlobName) : IRequest<PropertyMediaDto>;
