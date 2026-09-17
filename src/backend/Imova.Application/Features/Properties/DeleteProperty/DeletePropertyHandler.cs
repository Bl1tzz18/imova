using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.DeleteProperty;

public class DeletePropertyHandler(IApplicationDbContext dbContext) : IRequestHandler<DeletePropertyCommand, bool>
{
    public async Task<bool> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (property is null)
        {
            return false;
        }

        if (!request.IsAdmin && property.OwnerId != request.RequestingUserId)
        {
            throw new ForbiddenAccessException();
        }

        var location = await dbContext.PropertyLocations.FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);
        if (location is not null)
        {
            dbContext.PropertyLocations.Remove(location);
        }

        // No FK/cascade from PropertyMedia to Property (see PropertyMediaConfiguration), so these
        // have to be removed explicitly rather than relying on the database to cascade them.
        var media = await dbContext.PropertyMedias.Where(m => m.PropertyId == property.Id).ToListAsync(cancellationToken);
        dbContext.PropertyMedias.RemoveRange(media);

        dbContext.Properties.Remove(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
