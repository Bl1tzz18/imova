using Imova.Application.Features.Listings;

namespace Imova.UnitTests.Listings;

public class PropertyAddressTests
{
    [Fact]
    public void Compose_WithStreetAndBuildingNumber_FoldsThemIntoOneToken()
    {
        var result = PropertyAddress.Compose("Str. Ismail", "44", null, null, "Chisinau", "Moldova");

        Assert.Equal("Str. Ismail 44, Chisinau, Moldova", result);
    }

    [Fact]
    public void Compose_WithStreetAndNoBuildingNumber_OmitsTheNumber()
    {
        var result = PropertyAddress.Compose("Str. Ismail", null, null, null, "Chisinau", "Moldova");

        Assert.Equal("Str. Ismail, Chisinau, Moldova", result);
    }

    [Fact]
    public void Compose_WithBuildingNumberButNoStreet_OmitsTheNumberToo()
    {
        // A bare number with no street name isn't a usable geocoding token on its own.
        var result = PropertyAddress.Compose(null, "44", null, null, "Chisinau", "Moldova");

        Assert.Equal("Chisinau, Moldova", result);
    }

    [Fact]
    public void Compose_WithBlankBuildingNumber_OmitsIt()
    {
        var result = PropertyAddress.Compose("Str. Ismail", "   ", null, null, "Chisinau", "Moldova");

        Assert.Equal("Str. Ismail, Chisinau, Moldova", result);
    }

    [Fact]
    public void Compose_WithSectorAndDistrict_KeepsThemAfterTheStreetAndNumberToken()
    {
        var result = PropertyAddress.Compose("Str. Ismail", "44", "Botanica", "Ialoveni", "Chisinau", "Moldova");

        Assert.Equal("Str. Ismail 44, Botanica, Ialoveni, Chisinau, Moldova", result);
    }

    [Fact]
    public void Compose_WithOnlyCityAndCountry_ReturnsJustThose()
    {
        var result = PropertyAddress.Compose(null, null, null, null, "Chisinau", "Moldova");

        Assert.Equal("Chisinau, Moldova", result);
    }
}
