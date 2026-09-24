using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.RejectListing;

public class RejectListingHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<RejectListingCommand, ListingDto?>
{
    public async Task<ListingDto?> Handle(RejectListingCommand request, CancellationToken cancellationToken)
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

        ListingTransitions.Apply(() => listing.Reject(request.Reason));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(dbContext, blobStorageService, listing, currentUserId: null, cancellationToken);
    }
}
