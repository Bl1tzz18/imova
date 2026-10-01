using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Media.Sizes;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

// Runs after the client has already PUT the file directly to the SAS URL from RequestUploadUrl.
// This is the "did the upload actually happen, and is it what it claims to be" checkpoint —
// nothing here trusts the client-reported content-type or file extension.
public class ConfirmMediaUploadHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    PhotoSizeGenerator photoSizeGenerator,
    ILogger<ConfirmMediaUploadHandler> logger)
    : IRequestHandler<ConfirmMediaUploadCommand, PhotoDto>
{
    public async Task<PhotoDto> Handle(ConfirmMediaUploadCommand request, CancellationToken cancellationToken)
    {
        await MediaAccess.EnsureCanUploadAsync(dbContext, request.ListingId, request.RequestingUserId, request.IsAdmin, cancellationToken);

        var existing = await dbContext.Photos
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.BlobName == request.BlobName, cancellationToken);

        if (existing is not null)
        {
            // Confirming the same upload twice (e.g. a client retry after a dropped response) is
            // a no-op, not an error.
            return existing.ToDto(blobStorageService);
        }

        var blobInfo = await blobStorageService.TryGetUploadedBlobInfoAsync(request.BlobName, cancellationToken);

        if (blobInfo is null)
        {
            throw ValidationErrorFor("The file was not found in storage — the upload may not have completed.", ErrorCodes.UploadNotFound);
        }

        if (blobInfo.SizeBytes > Photo.MaxFileSizeBytes)
        {
            throw ValidationErrorFor($"File exceeds the {Photo.MaxFileSizeBytes / (1024 * 1024)}MB limit.", ErrorCodes.UploadTooLarge, CodedFailure.Params(("maxMb", Photo.MaxFileSizeBytes / (1024 * 1024))));
        }

        var detectedContentType = ImageSignature.DetectContentType(blobInfo.LeadingBytes);

        if (detectedContentType is null)
        {
            throw ValidationErrorFor("The uploaded file is not a recognized image format (JPEG, PNG, WebP).", ErrorCodes.UploadNotAnImage);
        }

        var sortOrder = await dbContext.Photos
            .Where(p => p.ListingId == request.ListingId)
            .CountAsync(cancellationToken);

        // The first photo of a listing becomes its cover image; see DeleteMediaHandler for how
        // that's handed on when the cover is removed.
        var photo = Photo.Create(
            request.ListingId,
            request.BlobName,
            detectedContentType,
            blobInfo.SizeBytes,
            sortOrder,
            isPrimary: sortOrder == 0,
            uploadedByUserId: request.RequestingUserId);

        // The display sizes right away, so nobody is ever served the full original. A file that
        // only looks like an image is refused here; a storage hiccup just leaves the sizes to the
        // Worker's backfill (the original is shown until then).
        try
        {
            var outcome = await photoSizeGenerator.GenerateAsync(photo, cancellationToken);
            if (outcome == PhotoSizeOutcome.Unreadable)
            {
                throw ValidationErrorFor("The uploaded file could not be read as an image.", ErrorCodes.UploadNotAnImage);
            }

            if (outcome == PhotoSizeOutcome.SourceMissing)
            {
                throw ValidationErrorFor("The file was not found in storage — the upload may not have completed.", ErrorCodes.UploadNotFound);
            }
        }
        catch (Exception ex) when (ex is not ValidationException and not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not make the display sizes of {BlobName} on upload; the backfill will.", request.BlobName);
        }

        dbContext.Photos.Add(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return photo.ToDto(blobStorageService);
    }

    private static ValidationException ValidationErrorFor(string message, string code, IReadOnlyDictionary<string, object>? parameters = null) =>
        new([CodedFailure.Of(nameof(ConfirmMediaUploadCommand.BlobName), message, code, parameters)]);
}
