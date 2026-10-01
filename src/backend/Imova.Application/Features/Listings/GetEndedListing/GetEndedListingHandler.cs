using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetEndedListing;

public class GetEndedListingHandler(IApplicationDbContext dbContext) : IRequestHandler<GetEndedListingQuery, EndedListingDto?>
{
    public async Task<EndedListingDto?> Handle(GetEndedListingQuery request, CancellationToken cancellationToken)
    {
        var row = await (
                from l in dbContext.Listings.AsNoTracking()
                join p in dbContext.Properties.AsNoTracking() on l.PropertyId equals p.Id
                join loc in dbContext.PropertyLocations.AsNoTracking() on p.LocationId equals loc.Id
                where l.Id == request.ListingId && EndedListingStatuses.All.Contains(l.Status)
                select new { Listing = l, Property = p, Location = loc })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var (listing, property, location) = (row.Listing, row.Property, row.Location);
        return new EndedListingDto(
            listing.Id,
            listing.Status.ToString(),
            listing.TransactionType.ToString(),
            property.PropertyType.ToString(),
            listing.Title,
            listing.Price.ToDto(),
            property.TotalAreaM2,
            location.RaionId,
            location.RaionName,
            location.LocalitateName,
            location.ChisinauSectorName,
            listing.UpdatedAt);
    }
}
