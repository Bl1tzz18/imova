using Imova.Application.Common.Interfaces;
using Imova.Domain.Favorites;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.SaveFavorite;

public class SaveFavoriteHandler(IApplicationDbContext dbContext) : IRequestHandler<SaveFavoriteCommand, bool>
{
    public async Task<bool> Handle(SaveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var propertyExists = await dbContext.Properties.AnyAsync(p => p.Id == request.PropertyId, cancellationToken);
        if (!propertyExists)
        {
            return false;
        }

        var alreadySaved = await dbContext.Favorites
            .AnyAsync(f => f.UserId == request.UserId && f.PropertyId == request.PropertyId, cancellationToken);

        if (!alreadySaved)
        {
            dbContext.Favorites.Add(Favorite.Create(request.UserId, request.PropertyId));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
