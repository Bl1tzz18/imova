using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetEndedListing;

// The little that stays public about an ended listing (EndedListingDto) — what its 410 page shows,
// and what the "no longer available" email to people who saved it says. Listings that haven't ended
// (EndedListingStatuses) are left out.
public static class EndedListingSummaries
{
    public static async Task<Dictionary<Guid, EndedListingDto>> LoadAsync(
        IApplicationDbContext dbContext, IReadOnlyCollection<Guid> listingIds, CancellationToken cancellationToken)
    {
        if (listingIds.Count == 0)
        {
            return [];
        }

        var rows = await (
                from l in dbContext.Listings.AsNoTracking()
                join p in dbContext.Properties.AsNoTracking() on l.PropertyId equals p.Id
                join loc in dbContext.PropertyLocations.AsNoTracking() on p.LocationId equals loc.Id
                where listingIds.Contains(l.Id) && EndedListingStatuses.All.Contains(l.Status)
                select new { Listing = l, Property = p, Location = loc })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Listing.Id,
            r => new EndedListingDto(
                r.Listing.Id,
                r.Listing.Status.ToString(),
                r.Listing.TransactionType.ToString(),
                r.Property.PropertyType.ToString(),
                r.Listing.Title,
                r.Listing.Price.ToDto(),
                r.Property.TotalAreaM2,
                r.Location.RaionId,
                r.Location.RaionName,
                r.Location.LocalitateName,
                r.Location.ChisinauSectorName,
                r.Listing.UpdatedAt));
    }
}
