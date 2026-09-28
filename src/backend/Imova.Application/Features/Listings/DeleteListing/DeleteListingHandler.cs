using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Listings.DeleteListing;

public class DeleteListingHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ILogger<DeleteListingHandler> logger) : IRequestHandler<DeleteListingCommand, bool>
{
    public async Task<bool> Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (listing is null)
        {
            return false;
        }

        await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);

        var favorites = await dbContext.Favorites.Where(f => f.ListingId == listing.Id).ToListAsync(cancellationToken);
        dbContext.Favorites.RemoveRange(favorites);

        // No FK/cascade from Photo to Listing (see PhotoConfiguration), so these have to be
        // removed explicitly rather than relying on the database to cascade them.
        var photos = await dbContext.Photos.Where(p => p.ListingId == listing.Id).ToListAsync(cancellationToken);
        dbContext.Photos.RemoveRange(photos);

        dbContext.Listings.Remove(listing);

        // The physical property (and its location) only goes too when no other listing — an
        // earlier sale, a parallel rental — still points at it.
        var propertyStillListed = await dbContext.Listings.AnyAsync(
            l => l.PropertyId == listing.PropertyId && l.Id != listing.Id, cancellationToken);
        if (!propertyStillListed)
        {
            var property = await dbContext.Properties
                .Include(p => p.Amenities)
                .Include(p => p.Proximities)
                .FirstOrDefaultAsync(p => p.Id == listing.PropertyId, cancellationToken);
            if (property is not null)
            {
                var location = await dbContext.PropertyLocations
                    .FirstOrDefaultAsync(l => l.Id == property.LocationId, cancellationToken);
                dbContext.Properties.Remove(property);
                if (location is not null)
                {
                    dbContext.PropertyLocations.Remove(location);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // The files go after the rows, so a failure here can only leave an unreferenced blob behind,
        // never a photo row pointing at a missing file.
        foreach (var photo in photos)
        {
            try
            {
                await blobStorageService.DeleteAsync(photo.BlobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete blob {BlobName} of deleted listing {ListingId}.", photo.BlobName, listing.Id);
            }
        }

        return true;
    }
}
