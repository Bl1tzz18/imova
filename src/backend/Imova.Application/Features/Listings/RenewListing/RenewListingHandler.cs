using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.RenewListing;

public class RenewListingHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<RenewListingCommand, ListingDto?>
{
    public async Task<ListingDto?> Handle(RenewListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);

        ListingTransitions.Apply(listing.Renew);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(dbContext, blobStorageService, listing, request.RequestingUserId, cancellationToken);
    }
}
