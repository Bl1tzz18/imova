using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.SuspendListing;

public class SuspendListingHandler(IApplicationDbContext dbContext) : IRequestHandler<SuspendListingCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(SuspendListingCommand request, CancellationToken cancellationToken)
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

        if (property.Status != PropertyStatus.Published)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(SuspendListingCommand.Id), "Only a published listing can be suspended.")]);
        }

        property.Suspend(request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location);
    }
}
