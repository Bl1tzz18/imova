using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.SubmitForReview;

public class SubmitForReviewHandler(IApplicationDbContext dbContext) : IRequestHandler<SubmitForReviewCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
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

        if (property.Status is not (PropertyStatus.Draft or PropertyStatus.Rejected))
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(SubmitForReviewCommand.Id), "Only a draft or rejected listing can be submitted for review.")]);
        }

        property.SubmitForReview();
        await dbContext.SaveChangesAsync(cancellationToken);

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location);
    }
}
