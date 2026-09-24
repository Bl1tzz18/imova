using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.SubmitListingForReview;

public class SubmitListingForReviewHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<SubmitListingForReviewCommand, ListingDto?>
{
    public async Task<ListingDto?> Handle(SubmitListingForReviewCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);

        ListingTransitions.Apply(listing.SubmitForReview);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(dbContext, blobStorageService, listing, request.RequestingUserId, cancellationToken);
    }
}
