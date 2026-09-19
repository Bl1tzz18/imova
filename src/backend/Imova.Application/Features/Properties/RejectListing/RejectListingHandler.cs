using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.RejectListing;

public class RejectListingHandler(IApplicationDbContext dbContext) : IRequestHandler<RejectListingCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(RejectListingCommand request, CancellationToken cancellationToken)
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

        if (property.Status != PropertyStatus.PendingReview)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(RejectListingCommand.Id), "Only a listing pending review can be rejected.")]);
        }

        property.Reject(request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location);
    }
}
