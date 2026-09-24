using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

public record ConfirmMediaUploadCommand(Guid ListingId, string BlobName) : IRequest<PhotoDto>;
