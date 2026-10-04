using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.GetEndedListing;
using Imova.Domain.Favorites;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.SaveFavorite;

public class SaveFavoriteHandler(IApplicationDbContext dbContext) : IRequestHandler<SaveFavoriteCommand, bool>
{
    public async Task<bool> Handle(SaveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.AsNoTracking()
            .Where(l => l.Id == request.ListingId)
            .Select(l => new { l.Price, l.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (listing is null)
        {
            return false;
        }

        var alreadySaved = await dbContext.Favorites
            .AnyAsync(f => f.UserId == request.UserId && f.ListingId == request.ListingId, cancellationToken);

        if (!alreadySaved)
        {
            // The price they see now is the one later price-change emails compare with; a listing
            // that has already ended has nothing left to tell them (see Favorite).
            dbContext.Favorites.Add(Favorite.Create(
                request.UserId, request.ListingId, listing.Price, EndedListingStatuses.All.Contains(listing.Status)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
