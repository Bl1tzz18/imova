using Imova.Application.Common.Interfaces;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.SearchListings;

public class SearchListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, IListingSearch listingSearch)
    : IRequestHandler<SearchListingsQuery, PagedResult<ListingDto>>
{
    public async Task<PagedResult<ListingDto>> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        var (ids, total) = await listingSearch.SearchAsync(request, cancellationToken);

        var byId = await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        var ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        var items = await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, ordered, request.CurrentUserId, cancellationToken);
        return new PagedResult<ListingDto>(items, request.Page, request.PageSize, total);
    }
}
