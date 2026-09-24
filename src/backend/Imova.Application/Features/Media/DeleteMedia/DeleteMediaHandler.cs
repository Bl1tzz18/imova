using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
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

        // Photos uploaded for a listing that was never actually created have no owner to check
        // against — treated as not found, same as before listings/publishers existed.
        var listing = await dbContext.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);

        if (listing is null)
        {
            return false;
        }

        await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);

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

        await blobStorageService.DeleteAsync(photo.BlobName, cancellationToken);

        return true;
    }
}
