using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

// Runs after the client has already PUT the file directly to the SAS URL from RequestUploadUrl.
// This is the "did the upload actually happen, and is it what it claims to be" checkpoint —
// nothing here trusts the client-reported content-type or file extension.
public class ConfirmMediaUploadHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<ConfirmMediaUploadCommand, PhotoDto>
{
    public async Task<PhotoDto> Handle(ConfirmMediaUploadCommand request, CancellationToken cancellationToken)
    {
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
            throw ValidationErrorFor("The file was not found in storage — the upload may not have completed.");
        }

        if (blobInfo.SizeBytes > Photo.MaxFileSizeBytes)
        {
            throw ValidationErrorFor($"File exceeds the {Photo.MaxFileSizeBytes / (1024 * 1024)}MB limit.");
        }

        var detectedContentType = ImageSignature.DetectContentType(blobInfo.LeadingBytes);

        if (detectedContentType is null)
        {
            throw ValidationErrorFor("The uploaded file is not a recognized image format (JPEG, PNG, WebP).");
        }

        var sortOrder = await dbContext.Photos
            .Where(p => p.ListingId == request.ListingId)
            .CountAsync(cancellationToken);

        // The first photo of a listing becomes its cover image; see DeleteMediaHandler for how
        // that's handed on when the cover is removed.
        var photo = Photo.Create(
            request.ListingId, request.BlobName, detectedContentType, blobInfo.SizeBytes, sortOrder, isPrimary: sortOrder == 0);

        dbContext.Photos.Add(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return photo.ToDto(blobStorageService);
    }

    private static ValidationException ValidationErrorFor(string message) =>
        new([new ValidationFailure(nameof(ConfirmMediaUploadCommand.BlobName), message)]);
}
