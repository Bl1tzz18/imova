using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.ReinstateListing;

public class ReinstateListingHandler(IApplicationDbContext dbContext) : IRequestHandler<ReinstateListingCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(ReinstateListingCommand request, CancellationToken cancellationToken)
    {
        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (property is null)
        {
            return null;
        }

        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        if (property.Status != PropertyStatus.Suspended)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(ReinstateListingCommand.Id), "Only a suspended listing can be reinstated.")]);
        }

        property.Reinstate();
        await dbContext.SaveChangesAsync(cancellationToken);

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location);
    }
}
