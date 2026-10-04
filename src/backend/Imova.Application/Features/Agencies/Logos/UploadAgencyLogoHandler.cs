using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Logos;

// The file is checked by its bytes (JPEG, PNG or WebP only), turned into the two square JPEGs, both
// stored, and only then does the agency point at them; the previous logo's files go last.
public class UploadAgencyLogoHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IPhotoResizer photoResizer,
    TimeProvider timeProvider,
    ILogger<UploadAgencyLogoHandler> logger) : IRequestHandler<UploadAgencyLogoCommand, AgencyDto?>
{
    public async Task<AgencyDto?> Handle(UploadAgencyLogoCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.EnsureCanEdit(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        var contentType = ImageSignature.DetectContentType(request.Content);
        if (contentType is null || !AgencyLogo.AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationException(
                [CodedFailure.Of(nameof(UploadAgencyLogoCommand.Content), "A logo must be a JPEG, PNG or WebP image.", ErrorCodes.AgencyLogoType)]);
        }

        var size = photoResizer.ReadSize(request.Content) ?? throw NotAnImage();
        if (Math.Max(size.Width, size.Height) < AgencyLogo.MinSide)
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    nameof(UploadAgencyLogoCommand.Content),
                    $"A logo must be at least {AgencyLogo.MinSide}×{AgencyLogo.MinSide} pixels.",
                    ErrorCodes.AgencyLogoTooSmall,
                    CodedFailure.Params(("min", AgencyLogo.MinSide))),
            ]);
        }

        var copies = await photoResizer.ResizeToSquareJpegAsync(
            new MemoryStream(request.Content), [AgencyLogo.Size, AgencyLogo.ThumbnailSize], cancellationToken)
            ?? throw NotAnImage();

        var now = timeProvider.GetUtcNow();
        var blobName = AgencyLogo.BlobName(agency.Id, now);
        await blobStorageService.UploadAsync(blobName, new MemoryStream(copies[0]), "image/jpeg", cancellationToken);
        await blobStorageService.UploadAsync(AgencyLogo.ThumbnailBlobName(blobName), new MemoryStream(copies[1]), "image/jpeg", cancellationToken);

        var previous = agency.SetLogo(blobName, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previous is not null)
        {
            await AgencyLogo.DeleteFilesAsync(blobStorageService, previous, logger, cancellationToken);
        }

        return await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, request.IsAdmin, cancellationToken);
    }

    private static ValidationException NotAnImage() =>
        new([CodedFailure.Of(nameof(UploadAgencyLogoCommand.Content), "The file could not be read as an image.", ErrorCodes.UploadNotAnImage)]);
}
