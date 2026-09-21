using Imova.Application.Features.Properties;
using Imova.Domain.Locations;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

// PropertyMapping.ToDto is the single place every property response is built (detail page,
// search, map, favorites, my-listings) — this confirms it always maps the real address/
// coordinates through, with no masking or rounding for any viewer.
public class PropertyMappingTests
{
    private static readonly Guid RaionId = Guid.NewGuid();
    private static readonly Guid LocalitateId = Guid.NewGuid();
    private static readonly Guid ChisinauSectorId = Guid.NewGuid();

    private static Property NewProperty() =>
        Property.Create(Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR");

    private static PropertyLocation NewLocation(Property property) =>
        PropertyLocation.Create(
            property.Id, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", null, null,
            47.01055, 28.86383, "Str. Ismail 44");

    [Fact]
    public void ToDto_AlwaysExposesTheExactCoordinatesAndStreet()
    {
        var property = NewProperty();
        var location = NewLocation(property);

        var dto = property.ToDto(location);

        Assert.Equal(47.01055, dto.Location!.Latitude);
        Assert.Equal(28.86383, dto.Location.Longitude);
        Assert.Equal("Str. Ismail 44", dto.Location.Street);
        Assert.Equal(RaionId, dto.Location.RaionId);
        Assert.Equal("Chisinau", dto.Location.RaionName);
        Assert.Equal(LocalitateId, dto.Location.LocalitateId);
        Assert.Equal("Botanica", dto.Location.LocalitateName);
    }

    [Fact]
    public void ToDto_ExposesChisinauSectorFields()
    {
        var property = NewProperty();
        var location = PropertyLocation.Create(
            property.Id, "Moldova", RaionId, "Chisinau", null, null, ChisinauSectorId, "Botanica",
            47.01055, 28.86383, "Str. Ismail 44");

        var dto = property.ToDto(location);

        Assert.Equal(ChisinauSectorId, dto.Location!.ChisinauSectorId);
        Assert.Equal("Botanica", dto.Location.ChisinauSectorName);
    }

    [Fact]
    public void ToDto_WithNullCoordinates_StaysNull()
    {
        var property = NewProperty();
        var location = PropertyLocation.Create(property.Id, "Moldova", RaionId, "Chisinau", null, null, null, null, null, null);

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
