using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Media;
using Imova.Domain.Media;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

// Runs after the client has already PUT the file directly to the SAS URL from RequestUploadUrl.
// This is the "did the upload actually happen, and is it what it claims to be" checkpoint —
// nothing here trusts the client-reported content-type or file extension.
public class ConfirmMediaUploadHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<ConfirmMediaUploadCommand, PropertyMediaDto>
{
    public async Task<PropertyMediaDto> Handle(ConfirmMediaUploadCommand request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.PropertyMedias
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

        if (blobInfo.SizeBytes > PropertyMedia.MaxFileSizeBytes)
        {
            throw ValidationErrorFor($"File exceeds the {PropertyMedia.MaxFileSizeBytes / (1024 * 1024)}MB limit.");
        }

        var detectedContentType = ImageSignature.DetectContentType(blobInfo.LeadingBytes);

        if (detectedContentType is null)
        {
            throw ValidationErrorFor("The uploaded file is not a recognized image format (JPEG, PNG, WebP).");
        }

        var sortOrder = await dbContext.PropertyMedias
            .Where(m => m.PropertyId == request.PropertyId)
            .CountAsync(cancellationToken);

        var media = PropertyMedia.Create(request.PropertyId, request.BlobName, detectedContentType, blobInfo.SizeBytes, sortOrder);

        dbContext.PropertyMedias.Add(media);
        await dbContext.SaveChangesAsync(cancellationToken);

        return media.ToDto(blobStorageService);
    }

    private static ValidationException ValidationErrorFor(string message) =>
        new([new ValidationFailure(nameof(ConfirmMediaUploadCommand.BlobName), message)]);
}
