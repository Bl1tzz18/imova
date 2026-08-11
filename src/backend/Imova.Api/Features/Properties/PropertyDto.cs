using Imova.Domain.Properties;

namespace Imova.Api.Features.Properties;

public record PropertyDto(Guid Id, string Title, decimal Price, string Currency, string City, string District)
{
    public static PropertyDto FromEntity(Property property) =>
        new(property.Id, property.Title, property.Price, property.Currency, property.City, property.District);
}
