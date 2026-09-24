using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetMyListings;

public class GetMyListingsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetMyListingsQuery, List<ListingDto>>
{
    public async Task<List<ListingDto>> Handle(GetMyListingsQuery request, CancellationToken cancellationToken)
    {
        var myPublisherIds = dbContext.Publishers
            .Where(p => p.UserId == request.UserId)
            .Select(p => p.Id);

        var listings = await dbContext.Listings
            .AsNoTracking()
            .Where(l => myPublisherIds.Contains(l.PublisherId))
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, listings, request.UserId, cancellationToken);
    }
}
