using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.ReinstateListing;

public class ReinstateListingHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<ReinstateListingCommand, ListingDto?>
{
    public async Task<ListingDto?> Handle(ReinstateListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        ListingTransitions.Apply(listing.Reinstate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(dbContext, blobStorageService, listing, currentUserId: null, cancellationToken);
    }
}
