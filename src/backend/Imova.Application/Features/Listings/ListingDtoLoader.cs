using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Builds ListingDtos for an already-loaded set of listings: one batched query each for their
// properties (+ amenities, proximities), locations, publishers, photos, and the caller's favorites, instead of
// every listing-returning handler repeating those joins. Output order matches the input order.
public static class ListingDtoLoader
{
    public static async Task<List<ListingDto>> LoadAsync(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        IReadOnlyList<Listing> listings,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        // Publisher phone/email are only exposed on a listing's own detail view — never on
        // cards/search results, so they can't be scraped in bulk.
        bool includeContactDetails = false)
    {
        if (listings.Count == 0)
        {
            return [];
        }

        var listingIds = listings.Select(l => l.Id).ToList();
        var propertyIds = listings.Select(l => l.PropertyId).Distinct().ToList();
        var publisherIds = listings.Select(l => l.PublisherId).Distinct().ToList();

        var propertiesById = await dbContext.Properties
            .AsNoTracking()
            .Include(p => p.Amenities)
            .Include(p => p.Proximities)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var locationIds = propertiesById.Values.Select(p => p.LocationId).ToList();
        var locationsById = await dbContext.PropertyLocations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var publishersById = await dbContext.Publishers
            .AsNoTracking()
            .Where(p => publisherIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Small seeded reference tables — cheaper to load whole than to collect ids first.
        var amenitiesById = await dbContext.Amenities.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var proximitiesById = await dbContext.Proximities.AsNoTracking().ToDictionaryAsync(p => p.Id, cancellationToken);

        var photosByListingId = (await dbContext.Photos
                .AsNoTracking()
                .Where(p => listingIds.Contains(p.ListingId))
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(p => p.ListingId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PhotoDto>)g.Select(p => p.ToDto(blobStorageService)).ToList());

        var savedListingIds = currentUserId is null
            ? []
            : await dbContext.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == currentUserId && listingIds.Contains(f.ListingId))
                .Select(f => f.ListingId)
                .ToHashSetAsync(cancellationToken);

        return listings
            .Select(listing =>
            {
                var property = propertiesById[listing.PropertyId];
                return listing.ToDto(
                    property,
                    locationsById.GetValueOrDefault(property.LocationId),
                    publishersById[listing.PublisherId],
                    amenitiesById,
                    proximitiesById,
                    photosByListingId.GetValueOrDefault(listing.Id) ?? [],
                    savedListingIds.Contains(listing.Id),
                    includeContactDetails);
            })
            .ToList();
    }

    public static async Task<ListingDto> LoadOneAsync(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        Listing listing,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        bool includeContactDetails = false) =>
        (await LoadAsync(dbContext, blobStorageService, [listing], currentUserId, cancellationToken, includeContactDetails))[0];
}
