using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.RemoveFavorite;

public class RemoveFavoriteHandler(IApplicationDbContext dbContext) : IRequestHandler<RemoveFavoriteCommand>
{
    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var favorite = await dbContext.Favorites
            .FirstOrDefaultAsync(f => f.UserId == request.UserId && f.PropertyId == request.PropertyId, cancellationToken);

        if (favorite is not null)
        {
            dbContext.Favorites.Remove(favorite);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
