using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Media.Sizes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Media.DeleteMedia;

public class DeleteMediaHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<DeleteMediaCommand, bool>
{
    public async Task<bool> Handle(DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        var photo = await dbContext.Photos
            .FirstOrDefaultAsync(p => p.Id == request.MediaId && p.ListingId == request.ListingId, cancellationToken);

        if (photo is null)
        {
            return false;
        }

        var listing = await dbContext.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);

        if (listing is not null)
        {
            await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);
            // A live or in-review listing keeps its MinPhotos: add a photo before removing one.
            await ListingPhotoRules.EnsureCanRemoveOneAsync(dbContext, listing, cancellationToken);
        }
        else if (!request.IsAdmin && photo.UploadedByUserId != request.RequestingUserId)
        {
            // Listing not created yet (the add-listing form removing a photo it just uploaded):
            // only its uploader may delete it. Photos from before uploaders were recorded have
            // none, so they stay not-found here, as they always were.
            return false;
        }

        dbContext.Photos.Remove(photo);

        // Hand the cover-image role on to the next photo in order, so a listing with photos
        // always has one.
        if (photo.IsPrimary)
        {
            var next = await dbContext.Photos
                .Where(p => p.ListingId == request.ListingId && p.Id != photo.Id)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            next?.MarkAsPrimary();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var blobName in PhotoSizes.AllBlobNames(photo))
        {
            await blobStorageService.DeleteAsync(blobName, cancellationToken);
        }

        return true;
    }
}
