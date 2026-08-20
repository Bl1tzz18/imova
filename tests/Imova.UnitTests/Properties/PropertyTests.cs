using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

public class PropertyTests
{
    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var property = Property.Create("Apartament 2 camere", 550m, "EUR", "Chisinau", "Botanica");

        Assert.NotEqual(Guid.Empty, property.Id);
        Assert.Equal("Apartament 2 camere", property.Title);
        Assert.Equal(550m, property.Price);
        Assert.Equal("EUR", property.Currency);
        Assert.Equal("Chisinau", property.City);
        Assert.Equal("Botanica", property.District);
    }

    [Theory]
    [InlineData("", 550, "EUR", "Chisinau", "Botanica")]
    [InlineData("Titlu", 0, "EUR", "Chisinau", "Botanica")]
    [InlineData("Titlu", -1, "EUR", "Chisinau", "Botanica")]
    [InlineData("Titlu", 550, "", "Chisinau", "Botanica")]
    [InlineData("Titlu", 550, "EUR", "", "Botanica")]
    [InlineData("Titlu", 550, "EUR", "Chisinau", "")]
    public void Create_WithInvalidData_Throws(string title, decimal price, string currency, string city, string district)
    {
        Assert.ThrowsAny<ArgumentException>(() => Property.Create(title, price, currency, city, district));
    }
}
