using Imova.Domain.Locations;

namespace Imova.UnitTests.Locations;

public class PropertyLocationTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();
    private static readonly Guid RaionId = Guid.NewGuid();
    private static readonly Guid LocalitateId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsAllFieldsAndBuildsAPoint()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638);

        Assert.NotEqual(Guid.Empty, location.Id);
        Assert.Equal(PropertyId, location.PropertyId);
        Assert.Equal("Moldova", location.Country);
        Assert.Equal(RaionId, location.RaionId);
        Assert.Equal("Chisinau", location.RaionName);
        Assert.Equal(LocalitateId, location.LocalitateId);
        Assert.Equal("Botanica", location.LocalitateName);
        Assert.Equal(47.0105, location.Latitude);
        Assert.Equal(28.8638, location.Longitude);
        Assert.Equal(28.8638, location.Location!.X);
        Assert.Equal(47.0105, location.Location.Y);
        Assert.Equal(4326, location.Location.SRID);
    }

    [Fact]
    public void Create_WithNullLocalitate_Succeeds()
    {
        var location = PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, null, 47.0105, 28.8638);

        Assert.Null(location.LocalitateId);
        Assert.Null(location.LocalitateName);
    }

    [Fact]
    public void Create_WithEmptyPropertyId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(Guid.Empty, "Moldova", RaionId, "Chisinau", null, null, 47.0105, 28.8638));
    }

    [Fact]
    public void Create_WithMissingCountry_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, "", RaionId, "Chisinau", null, null, 47.0105, 28.8638));
    }

    [Fact]
    public void Create_WithEmptyRaionId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", Guid.Empty, "Chisinau", null, null, 47.0105, 28.8638));
    }

    [Fact]
    public void Create_WithBlankRaionName_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", RaionId, "", null, null, 47.0105, 28.8638));
    }

    [Fact]
    public void Create_WithLocalitateIdButNoLocalitateName_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, null, 47.0105, 28.8638));
    }

    [Fact]
    public void Create_WithLocalitateNameButNoLocalitateId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, "Botanica", 47.0105, 28.8638));
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Create_WithLatitudeOutOfRange_Throws(double latitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, null, latitude, 28.8638));
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Create_WithLongitudeOutOfRange_Throws(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, null, 47.0105, longitude));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Create_WithLatitudeAtBoundary_Succeeds(double latitude)
    {
        var location = PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, null, latitude, 28.8638);

        Assert.Equal(latitude, location.Latitude);
    }

    [Fact]
    public void UpdateDetails_ChangesFieldsAndRebuildsThePoint()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638);

        var newRaionId = Guid.NewGuid();
        var newLocalitateId = Guid.NewGuid();
        location.UpdateDetails("Moldova", newRaionId, "Balti", newLocalitateId, "Centru", 47.75, 27.9167);

        Assert.Equal(newRaionId, location.RaionId);
        Assert.Equal("Balti", location.RaionName);
        Assert.Equal(newLocalitateId, location.LocalitateId);
        Assert.Equal("Centru", location.LocalitateName);
        Assert.Equal(47.75, location.Latitude);
        Assert.Equal(27.9167, location.Longitude);
        Assert.Equal(27.9167, location.Location!.X);
        Assert.Equal(47.75, location.Location.Y);
    }

    [Fact]
    public void UpdateDetails_WithMissingCountry_Throws()
    {
        var location = PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, null, 47.0105, 28.8638);

        Assert.ThrowsAny<ArgumentException>(() =>
            location.UpdateDetails("", RaionId, "Chisinau", null, null, 47.0105, 28.8638));
    }

    [Fact]
    public void Create_WithNullCoordinates_SucceedsWithNoLocationPoint()
    {
        // Geocoding failing (or not having run yet) must not block creating the listing — see
        // IGeocodingService.
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", null, null);

        Assert.Null(location.Latitude);
        Assert.Null(location.Longitude);
        Assert.Null(location.Location);
    }

    [Theory]
    [InlineData(47.0105, null)]
    [InlineData(null, 28.8638)]
    public void Create_WithOnlyOneCoordinateSet_Throws(double? latitude, double? longitude)
    {
        Assert.Throws<ArgumentException>(() =>
            PropertyLocation.Create(PropertyId, "Moldova", RaionId, "Chisinau", null, null, latitude, longitude));
    }

    [Fact]
    public void UpdateDetails_WithNullCoordinates_ClearsTheLocationPoint()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638);

        location.UpdateDetails("Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", null, null);

        Assert.Null(location.Latitude);
        Assert.Null(location.Longitude);
        Assert.Null(location.Location);
    }

    [Fact]
    public void Create_WithStreet_SetsStreet()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638, "Str. Ismail 44");

        Assert.Equal("Str. Ismail 44", location.Street);
    }

    [Fact]
    public void Create_WithoutStreet_LeavesStreetNull()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638);

        Assert.Null(location.Street);
    }

    [Fact]
    public void UpdateDetails_WithStreet_ChangesStreet()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638, "Str. Ismail 44");

        location.UpdateDetails(
            "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638, "Str. Alba Iulia 12");

        Assert.Equal("Str. Alba Iulia 12", location.Street);
    }

    [Fact]
    public void UpdateDetails_WithoutStreet_ClearsStreet()
    {
        var location = PropertyLocation.Create(
            PropertyId, "Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638, "Str. Ismail 44");

        location.UpdateDetails("Moldova", RaionId, "Chisinau", LocalitateId, "Botanica", 47.0105, 28.8638);

        Assert.Null(location.Street);
    }
}
