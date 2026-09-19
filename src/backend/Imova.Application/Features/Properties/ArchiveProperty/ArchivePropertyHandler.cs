using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.ArchiveProperty;

public class ArchivePropertyHandler(IApplicationDbContext dbContext) : IRequestHandler<ArchivePropertyCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(ArchivePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (property is null)
        {
            return null;
        }

        if (!request.IsAdmin && property.OwnerId != request.RequestingUserId)
        {
            throw new ForbiddenAccessException();
        }

        // "Deactivate" only makes sense for a listing that has actually been live — a Draft,
        // PendingReview, or Rejected listing is discarded via delete instead, and an already
        // Archived one has nothing to do here. Mirrors Property.Archive()'s own guard.
        if (property.Status is not (PropertyStatus.Published or PropertyStatus.Rented or PropertyStatus.Sold))
        {
            throw new ValidationException([new ValidationFailure(nameof(ArchivePropertyCommand.Id), "Only a published, rented, or sold listing can be deactivated.")]);
        }

        property.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location);
    }
}
