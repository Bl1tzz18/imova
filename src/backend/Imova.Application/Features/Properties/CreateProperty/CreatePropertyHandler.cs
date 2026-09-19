using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.CreateProperty;

public class CreatePropertyHandler(IApplicationDbContext dbContext) : IRequestHandler<CreatePropertyCommand, PropertyDto>
{
    public async Task<PropertyDto> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = Property.Create(
            request.OwnerId,
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
            request.PetsAllowed,
            request.Id);

        // A new listing goes straight into the admin review queue — the owner doesn't take a
        // separate "submit for review" step for a listing they just finished creating.
        // SubmitForReview() stays available as its own command for the other case it's actually
        // needed: resubmitting after a Rejected listing has been fixed.
        property.SubmitForReview();

        var location = PropertyLocation.Create(
            property.Id,
            request.Country,
            request.City,
            request.District,
            request.Latitude,
            request.Longitude);

        dbContext.Properties.Add(property);
        dbContext.PropertyLocations.Add(location);
        await dbContext.SaveChangesAsync(cancellationToken);

        return property.ToDto(location);
    }
}
