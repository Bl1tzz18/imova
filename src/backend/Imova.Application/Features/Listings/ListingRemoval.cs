using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Everything a listing takes with it when it's deleted — by its owner (DeleteListing) or with the
// whole account (AccountDeletion): the favorites on it, its photo rows, the reports filed on it, its price history, and the physical Property
// (+ location) unless another listing still uses it. Marks the rows for removal without saving;
// the caller saves, then deletes the returned photo blobs (originals and their sizes) (files after rows, so a failure can only
// leave an unreferenced file behind, never a row pointing at a missing one).
public static class ListingRemoval
{
    public static async Task<IReadOnlyList<string>> RemoveAsync(
        IApplicationDbContext dbContext, IReadOnlyCollection<Listing> listings, CancellationToken cancellationToken)
    {
        if (listings.Count == 0)
        {
            return [];
        }

        var listingIds = listings.Select(l => l.Id).ToList();

        var favorites = await dbContext.Favorites.Where(f => listingIds.Contains(f.ListingId)).ToListAsync(cancellationToken);
        dbContext.Favorites.RemoveRange(favorites);

        // No FK/cascade from Photo to Listing (see PhotoConfiguration), so these have to be
        // removed explicitly rather than relying on the database to cascade them.
        var photos = await dbContext.Photos.Where(p => listingIds.Contains(p.ListingId)).ToListAsync(cancellationToken);
        dbContext.Photos.RemoveRange(photos);

        // The database cascades these too; removed here as well so the unit tests' in-memory
        // database (no cascades for untracked rows) behaves the same.
        dbContext.ListingReports.RemoveRange(
            await dbContext.ListingReports.Where(r => listingIds.Contains(r.ListingId)).ToListAsync(cancellationToken));
        dbContext.ListingPriceChanges.RemoveRange(
            await dbContext.ListingPriceChanges.Where(c => listingIds.Contains(c.ListingId)).ToListAsync(cancellationToken));

        dbContext.Listings.RemoveRange(listings);

        // The physical property (and its location) only goes too when no listing outside this
        // batch — an earlier sale, a parallel rental — still points at it.
        var propertyIds = listings.Select(l => l.PropertyId).Distinct().ToList();
        var stillListed = await dbContext.Listings
            .Where(l => propertyIds.Contains(l.PropertyId) && !listingIds.Contains(l.Id))
            .Select(l => l.PropertyId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var orphanedPropertyIds = propertyIds.Except(stillListed).ToList();

        var properties = await dbContext.Properties
            .Include(p => p.Amenities)
            .Include(p => p.Proximities)
            .Where(p => orphanedPropertyIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var locationIds = properties.Select(p => p.LocationId).ToList();
        var locations = await dbContext.PropertyLocations.Where(l => locationIds.Contains(l.Id)).ToListAsync(cancellationToken);
        dbContext.Properties.RemoveRange(properties);
        dbContext.PropertyLocations.RemoveRange(locations);

        return photos.SelectMany(PhotoSizes.AllBlobNames).ToList();
    }
}
