using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetEndedListing;

public class GetEndedListingHandler(IApplicationDbContext dbContext) : IRequestHandler<GetEndedListingQuery, EndedListingDto?>
{
    public async Task<EndedListingDto?> Handle(GetEndedListingQuery request, CancellationToken cancellationToken) =>
        (await EndedListingSummaries.LoadAsync(dbContext, [request.ListingId], cancellationToken))
        .GetValueOrDefault(request.ListingId);
}
