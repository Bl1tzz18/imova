using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Logos;
using Imova.Application.Features.Listings.PriceHistory;
using Imova.Contracts.Listings;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Builds ListingDtos for an already-loaded set of listings: one batched query each for their
// properties (+ amenities, proximities), locations, publishers, photos, price changes, the caller's favorites and (for the owner/admins) favorite counts, instead of
// every listing-returning handler repeating those joins. Output order matches the input order.
public static class ListingDtoLoader
{
    public static async Task<List<ListingDto>> LoadAsync(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        IReadOnlyList<Listing> listings,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        // Contact details (ListingDto.Contact) are only exposed on a listing's own detail view —
        // never on cards/search results, so they can't be scraped in bulk.
        bool includeContactDetails = false,
        // An admin (like the owner) still sees a phone number the owner chose to hide.
        bool viewerIsAdmin = false)
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

        // Contact details show the person behind each publisher (an agency's agent): their name and photo.
        var publisherUserIds = publishersById.Values.Select(p => p.UserId).Distinct().ToList();
        var peopleByUserId = includeContactDetails
            ? await dbContext.Users
                .AsNoTracking()
                .Where(u => publisherUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new PublisherPerson(u.DisplayName, u.ProfilePictureUrl), cancellationToken)
            : [];

        // The agency each listing is published under (ListingDto.Agency, on every view; also named in
        // its contact details), with its small logo.
        var agencyIds = listings.Select(l => l.AgencyId).OfType<Guid>().Distinct().ToList();
        var agenciesById = agencyIds.Count == 0
            ? []
            : (await dbContext.Agencies
                    .AsNoTracking()
                    .Where(a => agencyIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.Name, a.Slug, a.LogoBlobName, a.IsVerified })
                    .ToListAsync(cancellationToken))
                .ToDictionary(
                    a => a.Id,
                    a => new ListingAgencyDto(
                        a.Id,
                        a.Name,
                        a.Slug,
                        a.LogoBlobName is null ? null : blobStorageService.GetPublicUrl(AgencyLogo.ThumbnailBlobName(a.LogoBlobName)),
                        a.IsVerified));

        // Agencies whose listings the caller manages as an Owner or Admin (see ListingAccess): they see
        // those listings' private details and statistics, like the author.
        var managedAgencyIds = currentUserId is { } viewerId && agencyIds.Count > 0
            ? await dbContext.AgencyMembers
                .AsNoTracking()
                .Where(m => m.UserId == viewerId && agencyIds.Contains(m.AgencyId)
                    && (m.Role == AgencyRole.Owner || m.Role == AgencyRole.Admin))
                .Select(m => m.AgencyId)
                .ToHashSetAsync(cancellationToken)
            : [];
        bool Manages(Listing listing) =>
            viewerIsAdmin
            || (currentUserId is not null && publishersById[listing.PublisherId].UserId == currentUserId)
            || (listing.AgencyId is { } agencyId && managedAgencyIds.Contains(agencyId));

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

        // Price changes, for "Preț redus" (every view) and the price history (detail view only);
        // only published listings can have public ones.
        var publishedIds = listings.Where(l => l.PublishedAt != null).Select(l => l.Id).ToList();
        var priceChangesByListingId = publishedIds.Count == 0
            ? []
            : (await dbContext.ListingPriceChanges
                    .AsNoTracking()
                    .Where(c => publishedIds.Contains(c.ListingId))
                    .ToListAsync(cancellationToken))
                .GroupBy(c => c.ListingId)
                .ToDictionary(g => g.Key, g => g.ToList());
        var now = DateTimeOffset.UtcNow;

        var savedListingIds = currentUserId is null
            ? []
            : await dbContext.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == currentUserId && listingIds.Contains(f.ListingId))
                .Select(f => f.ListingId)
                .ToHashSetAsync(cancellationToken);

        // The owner's and admins' statistics (see ListingMapping's canSeeStats): how many people saved it.
        var statsListingIds = listings
            .Where(Manages)
            .Select(l => l.Id)
            .ToHashSet();
        var favoriteCounts = statsListingIds.Count == 0
            ? []
            : await dbContext.Favorites
                .AsNoTracking()
                .Where(f => statsListingIds.Contains(f.ListingId))
                .GroupBy(f => f.ListingId)
                .Select(g => new { ListingId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ListingId, g => g.Count, cancellationToken);

        return listings
            .Select(listing =>
            {
                var property = propertiesById[listing.PropertyId];
                var publisher = publishersById[listing.PublisherId];
                var agency = listing.AgencyId is { } agencyId ? agenciesById.GetValueOrDefault(agencyId) : null;
                var priceChanges = ListingPriceHistory.PublicChanges(
                    priceChangesByListingId.GetValueOrDefault(listing.Id) ?? [], listing.PublishedAt);
                var dto = listing.ToDto(
                    property,
                    locationsById.GetValueOrDefault(property.LocationId),
                    publisher,
                    amenitiesById,
                    proximitiesById,
                    photosByListingId.GetValueOrDefault(listing.Id) ?? [],
                    savedListingIds.Contains(listing.Id),
                    includeContactDetails,
                    canSeePrivateDetails: Manages(listing),
                    publisherPerson: peopleByUserId.GetValueOrDefault(publisher.UserId),
                    agency: agency is null ? null : new ListingAgency(agency.Name, agency.LogoUrl));
                return dto with
                {
                    Agency = agency,
                    PriceReduction = ListingPriceHistory.Reduction(priceChanges, listing.Price, now),
                    PriceHistory = includeContactDetails ? ListingPriceHistory.History(priceChanges) : null,
                    FavoriteCount = statsListingIds.Contains(listing.Id) ? favoriteCounts.GetValueOrDefault(listing.Id) : null,
                };
            })
            .ToList();
    }

    public static async Task<ListingDto> LoadOneAsync(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        Listing listing,
        Guid? currentUserId,
        CancellationToken cancellationToken,
        bool includeContactDetails = false,
        bool viewerIsAdmin = false) =>
        (await LoadAsync(
            dbContext, blobStorageService, [listing], currentUserId, cancellationToken, includeContactDetails, viewerIsAdmin))[0];
}
