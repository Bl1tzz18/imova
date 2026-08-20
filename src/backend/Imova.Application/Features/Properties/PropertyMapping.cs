using Imova.Contracts.Properties;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Properties;

public static class PropertyMapping
{
    public static PropertyDto ToDto(this Property property) =>
        new(property.Id, property.Title, property.Price, property.Currency, property.City, property.District);
}
