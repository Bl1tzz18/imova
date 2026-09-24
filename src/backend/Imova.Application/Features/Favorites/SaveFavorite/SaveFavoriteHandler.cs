using Imova.Application.Common.Interfaces;
using Imova.Domain.Favorites;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.SaveFavorite;

public class SaveFavoriteHandler(IApplicationDbContext dbContext) : IRequestHandler<SaveFavoriteCommand, bool>
{
    public async Task<bool> Handle(SaveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var listingExists = await dbContext.Listings.AnyAsync(l => l.Id == request.ListingId, cancellationToken);
        if (!listingExists)
        {
            return false;
        }

        var alreadySaved = await dbContext.Favorites
            .AnyAsync(f => f.UserId == request.UserId && f.ListingId == request.ListingId, cancellationToken);

        if (!alreadySaved)
        {
            dbContext.Favorites.Add(Favorite.Create(request.UserId, request.ListingId));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
