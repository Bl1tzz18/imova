using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.GetListingById;

public class GetListingByIdHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetListingByIdQuery, ListingDto?>
{
    public async Task<ListingDto?> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (listing is null)
        {
            return null;
        }

        // A non-Active listing is only visible to its own owner (e.g. the edit page) or an admin
        // (e.g. reviewing it from the moderation queue) — anyone else, including an anonymous
        // visitor, gets the same "not found" as if the row didn't exist. Matches GetListingsHandler.
        if (listing.Status != ListingStatus.Active
            && !request.IsAdmin
            && !await ListingAccess.IsOwnedByAsync(dbContext, listing, request.CurrentUserId, cancellationToken))
        {
            return null;
        }

        return await ListingDtoLoader.LoadOneAsync(
            dbContext, blobStorageService, listing, request.CurrentUserId, cancellationToken, includeContactDetails: true);
    }
}
