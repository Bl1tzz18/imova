using Imova.Contracts.Properties;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using MediatR;

namespace Imova.Application.Features.Properties.CreateProperty;

public class CreatePropertyHandler(ImovaDbContext dbContext) : IRequestHandler<CreatePropertyCommand, PropertyDto>
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
            request.Currency);

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
