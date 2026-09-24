using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetListings;

// Backs public browsing only (home page, search, map) — only Active listings, regardless of who's
// asking; see GetMyListingsHandler for "everything I own, any status".
public class GetListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetListingsQuery, List<ListingDto>>
{
    public async Task<List<ListingDto>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from listing in dbContext.Listings.AsNoTracking()
            join property in dbContext.Properties.AsNoTracking() on listing.PropertyId equals property.Id
            where listing.Status == ListingStatus.Active
            select new { listing, property };

        if (request.PropertyType is not null)
        {
            query = query.Where(x => x.property.PropertyType == request.PropertyType);
        }

        if (request.TransactionType is not null)
        {
            query = query.Where(x => x.listing.TransactionType == request.TransactionType);
        }

        if (request.MinPriceEur is not null)
        {
            query = query.Where(x => x.listing.Price.PriceEur >= request.MinPriceEur);
        }

        if (request.MaxPriceEur is not null)
        {
            query = query.Where(x => x.listing.Price.PriceEur <= request.MaxPriceEur);
        }

        var listings = await query
            .OrderByDescending(x => x.listing.PublishedAt)
            .Select(x => x.listing)
            .ToListAsync(cancellationToken);

        return await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, listings, request.CurrentUserId, cancellationToken);
    }
}
