using Imova.Domain.Locations;

namespace Imova.UnitTests.Locations;

public class PropertyLocationTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsAllFieldsAndBuildsAPoint()
    {
        var location = PropertyLocation.Create(PropertyId, "Moldova", "Chisinau", "Botanica", 47.0105, 28.8638);

        Assert.NotEqual(Guid.Empty, location.Id);
        Assert.Equal(PropertyId, location.PropertyId);
        Assert.Equal("Moldova", location.Country);
        Assert.Equal("Chisinau", location.City);
        Assert.Equal("Botanica", location.District);
        Assert.Equal(47.0105, location.Latitude);
        Assert.Equal(28.8638, location.Longitude);
        Assert.Equal(28.8638, location.Location.X);
        Assert.Equal(47.0105, location.Location.Y);
        Assert.Equal(4326, location.Location.SRID);
    }

    [Fact]
    public void Create_WithNullDistrict_Succeeds()
    {
        var location = PropertyLocation.Create(PropertyId, "Moldova", "Chisinau", null, 47.0105, 28.8638);

        Assert.Null(location.District);
    }

    [Fact]
    public void Create_WithEmptyPropertyId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(Guid.Empty, "Moldova", "Chisinau", null, 47.0105, 28.8638));
    }

    [Theory]
    [InlineData("", "Chisinau")]
    [InlineData("Moldova", "")]
    public void Create_WithMissingCountryOrCity_Throws(string country, string city)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, country, city, null, 47.0105, 28.8638));
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Create_WithLatitudeOutOfRange_Throws(double latitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", "Chisinau", null, latitude, 28.8638));
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Create_WithLongitudeOutOfRange_Throws(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", "Chisinau", null, 47.0105, longitude));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Create_WithLatitudeAtBoundary_Succeeds(double latitude)
    {
        var location = PropertyLocation.Create(PropertyId, "Moldova", "Chisinau", null, latitude, 28.8638);

        Assert.Equal(latitude, location.Latitude);
    }
}
