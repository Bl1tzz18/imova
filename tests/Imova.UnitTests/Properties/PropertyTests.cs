using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

public class PropertyTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var property = Property.Create(
            OwnerId,
            "Apartament 2 camere",
            "Apartament luminos, aproape de centru.",
            PropertyType.Apartment,
            ListingType.Rent,
            550m,
            "EUR");

        Assert.NotEqual(Guid.Empty, property.Id);
        Assert.Equal(OwnerId, property.OwnerId);
        Assert.Equal("Apartament 2 camere", property.Title);
        Assert.Equal(PropertyType.Apartment, property.PropertyType);
        Assert.Equal(ListingType.Rent, property.ListingType);
        Assert.Equal(PropertyStatus.Draft, property.Status);
        Assert.Equal(550m, property.Price);
        Assert.Equal("EUR", property.Currency);
    }

    [Theory]
    [InlineData("", "Descriere", 550, "EUR")]
    [InlineData("Titlu", "", 550, "EUR")]
    [InlineData("Titlu", "Descriere", 0, "EUR")]
    [InlineData("Titlu", "Descriere", -1, "EUR")]
    [InlineData("Titlu", "Descriere", 550, "")]
    public void Create_WithInvalidData_Throws(string title, string description, decimal price, string currency)
    {
        Assert.ThrowsAny<ArgumentException>(() => Property.Create(
            OwnerId, title, description, PropertyType.Apartment, ListingType.Rent, price, currency));
    }

    [Fact]
    public void Create_WithEmptyOwnerId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Property.Create(
            Guid.Empty, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR"));
    }

    [Fact]
    public void Publish_FromDraft_SetsPublishedStatusAndTimestamp()
    {
        var property = Property.Create(
            OwnerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR");

        property.Publish();

        Assert.Equal(PropertyStatus.Published, property.Status);
        Assert.NotNull(property.PublishedAt);
    }

    [Fact]
    public void Publish_WhenNotDraft_Throws()
    {
        var property = Property.Create(
            OwnerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR");
        property.Publish();

        Assert.Throws<InvalidOperationException>(() => property.Publish());
    }
}
