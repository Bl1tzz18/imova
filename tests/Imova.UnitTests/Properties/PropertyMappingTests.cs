using Imova.Application.Features.Properties;
using Imova.Domain.Locations;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

// PropertyMapping.ToDto is the single place every property response is built (detail page,
// search, map, favorites, my-listings) — this confirms it always maps the real address/
// coordinates through, with no masking or rounding for any viewer.
public class PropertyMappingTests
{
    private static Property NewProperty() =>
        Property.Create(Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR");

    private static PropertyLocation NewLocation(Property property) =>
        PropertyLocation.Create(property.Id, "Moldova", "Chisinau", "Botanica", 47.01055, 28.86383, "Str. Ismail 44");

    [Fact]
    public void ToDto_AlwaysExposesTheExactCoordinatesAndStreet()
    {
        var property = NewProperty();
        var location = NewLocation(property);

        var dto = property.ToDto(location);

        Assert.Equal(47.01055, dto.Location!.Latitude);
        Assert.Equal(28.86383, dto.Location.Longitude);
        Assert.Equal("Str. Ismail 44", dto.Location.Street);
        Assert.Equal("Chisinau", dto.Location.City);
        Assert.Equal("Botanica", dto.Location.District);
    }

    [Fact]
    public void ToDto_WithNullCoordinates_StaysNull()
    {
        var property = NewProperty();
        var location = PropertyLocation.Create(property.Id, "Moldova", "Chisinau", null, null, null);

        var dto = property.ToDto(location);

        Assert.Null(dto.Location!.Latitude);
        Assert.Null(dto.Location.Longitude);
    }

    [Fact]
    public void ToDto_WithNullLocation_ReturnsNullLocation()
    {
        var property = NewProperty();

        var dto = property.ToDto(null);

        Assert.Null(dto.Location);
    }
}
