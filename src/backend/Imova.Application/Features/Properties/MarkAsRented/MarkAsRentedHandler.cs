using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.MarkAsRented;

public class MarkAsRentedHandler(IApplicationDbContext dbContext) : IRequestHandler<MarkAsRentedCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(MarkAsRentedCommand request, CancellationToken cancellationToken)
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

        if (property.Status != PropertyStatus.Published)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(MarkAsRentedCommand.Id), "Only a published listing can be marked as rented.")]);
        }

        if (property.ListingType != ListingType.Rent)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(MarkAsRentedCommand.Id), "Only a rental listing can be marked as rented.")]);
        }

        property.MarkAsRented();
        await dbContext.SaveChangesAsync(cancellationToken);

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location);
    }
}
