using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.UpdateProperty;

public class UpdatePropertyHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdatePropertyCommand, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
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

        var location = await dbContext.PropertyLocations.FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        property.UpdateDetails(
            request.Title,
            request.Description,
            request.PropertyType,
            request.ListingType,
            request.Price,
            request.Currency,
            request.Area,
            request.Rooms,
            request.Bathrooms,
            request.Floor,
            request.TotalFloors,
            request.YearBuilt,
            request.Furnished,
            request.ParkingAvailable,
            request.PetsAllowed);

        // Every listing has a location row created alongside it in CreatePropertyHandler — this
        // null check is defensive, not an expected path.
        location?.UpdateDetails(request.Country, request.City, request.District, request.Latitude, request.Longitude);

        // Saving edits to a Rejected listing is the owner's way of addressing whatever an admin
        // flagged — resubmit it in the same step instead of making them press a separate button
        // (there is no standalone "submit for review" affordance for Rejected listings on the
        // frontend anymore; see OwnerListingsList.tsx).
        if (property.Status == PropertyStatus.Rejected)
        {
            property.SubmitForReview();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return property.ToDto(location);
    }
}
