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

    private static Property NewProperty(ListingType listingType = ListingType.Rent) =>
        Property.Create(OwnerId, "Titlu", "Descriere", PropertyType.Apartment, listingType, 550m, "EUR");

    // Draft -> PendingReview -> Published, the only way to reach Published now that self-publish
    // (the old Publish()) is gone — see CreatePropertyHandler, which calls SubmitForReview() right
    // after Property.Create() so a listing is never left sitting as a self-published Draft.
    private static Property NewPublishedProperty(ListingType listingType = ListingType.Rent)
    {
        var property = NewProperty(listingType);
        property.SubmitForReview();
        property.Approve();
        return property;
    }

    [Fact]
    public void UpdateDetails_ChangesTitleDescriptionAndPriceAndTouchesUpdatedAt()
    {
        var property = NewProperty();
        var originalUpdatedAt = property.UpdatedAt;

        property.UpdateDetails(
            "Titlu nou", "Descriere noua", PropertyType.Apartment, ListingType.Rent, 600m, "EUR",
            null, null, null, null, null, null, null, null, null);

        Assert.Equal("Titlu nou", property.Title);
        Assert.Equal("Descriere noua", property.Description);
        Assert.Equal(600m, property.Price);
        Assert.True(property.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateDetails_DoesNotChangeStatusOrOwner()
    {
        var property = NewPublishedProperty();

        property.UpdateDetails(
            "Titlu nou", "Descriere noua", PropertyType.Apartment, ListingType.Rent, 600m, "EUR",
            null, null, null, null, null, null, null, null, null);

        Assert.Equal(PropertyStatus.Published, property.Status);
        Assert.Equal(OwnerId, property.OwnerId);
    }

    [Fact]
    public void UpdateDetails_ChangesPropertyTypeListingTypeAndDetailFields()
    {
        var property = NewProperty();

        property.UpdateDetails(
            "Titlu", "Descriere", PropertyType.House, ListingType.Sale, 100000m, "EUR",
            area: 120m, rooms: 4m, bathrooms: 2, floor: null, totalFloors: 2,
            yearBuilt: 2010, furnished: true, parkingAvailable: true, petsAllowed: null);

        Assert.Equal(PropertyType.House, property.PropertyType);
        Assert.Equal(ListingType.Sale, property.ListingType);
        Assert.Equal(120m, property.Area);
        Assert.Equal(4m, property.Rooms);
        Assert.Equal((short)2, property.Bathrooms);
        Assert.Equal((short)2010, property.YearBuilt);
        Assert.True(property.Furnished);
        Assert.True(property.ParkingAvailable);
    }

    [Theory]
    [InlineData("", "Descriere", 550)]
    [InlineData("Titlu", "", 550)]
    [InlineData("Titlu", "Descriere", 0)]
    public void UpdateDetails_WithInvalidData_Throws(string title, string description, decimal price)
    {
        var property = NewProperty();

        Assert.ThrowsAny<ArgumentException>(() => property.UpdateDetails(
            title, description, PropertyType.Apartment, ListingType.Rent, price, "EUR",
            null, null, null, null, null, null, null, null, null));
    }

    [Fact]
    public void SubmitForReview_FromDraft_SetsPendingReview()
    {
        var property = NewProperty();

        property.SubmitForReview();

        Assert.Equal(PropertyStatus.PendingReview, property.Status);
    }

    [Fact]
    public void SubmitForReview_FromRejected_SetsPendingReviewAndClearsRejectionReason()
    {
        var property = NewProperty();
        property.SubmitForReview();
        property.Reject("Photos are too blurry.");

        property.SubmitForReview();

        Assert.Equal(PropertyStatus.PendingReview, property.Status);
        Assert.Null(property.RejectionReason);
    }

    [Fact]
    public void SubmitForReview_WhenNotDraftOrRejected_Throws()
    {
        var property = NewPublishedProperty();

        Assert.Throws<InvalidOperationException>(() => property.SubmitForReview());
    }

    [Fact]
    public void Approve_FromPendingReview_SetsPublishedAndTimestamp()
    {
        var property = NewProperty();
        property.SubmitForReview();

        property.Approve();

        Assert.Equal(PropertyStatus.Published, property.Status);
        Assert.NotNull(property.PublishedAt);
    }

    [Fact]
    public void Approve_WhenNotPendingReview_Throws()
    {
        var property = NewProperty();

        Assert.Throws<InvalidOperationException>(() => property.Approve());
    }

    [Fact]
    public void Reject_FromPendingReview_SetsRejectedAndStoresReason()
    {
        var property = NewProperty();
        property.SubmitForReview();

        property.Reject("Missing required photos.");

        Assert.Equal(PropertyStatus.Rejected, property.Status);
        Assert.Equal("Missing required photos.", property.RejectionReason);
    }

    [Fact]
    public void Reject_WhenNotPendingReview_Throws()
    {
        var property = NewProperty();

        Assert.Throws<InvalidOperationException>(() => property.Reject("Some reason."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithEmptyReason_Throws(string reason)
    {
        var property = NewProperty();
        property.SubmitForReview();

        Assert.Throws<ArgumentException>(() => property.Reject(reason));
    }

    [Fact]
    public void Suspend_FromPublished_SetsSuspendedAndStoresReason()
    {
        var property = NewPublishedProperty();

        property.Suspend("Reported as a duplicate listing.");

        Assert.Equal(PropertyStatus.Suspended, property.Status);
        Assert.Equal("Reported as a duplicate listing.", property.SuspensionReason);
    }

    [Fact]
    public void Suspend_WhenNotPublished_Throws()
    {
        var property = NewProperty();

        Assert.Throws<InvalidOperationException>(() => property.Suspend("Some reason."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Suspend_WithEmptyReason_Throws(string reason)
    {
        var property = NewPublishedProperty();

        Assert.Throws<ArgumentException>(() => property.Suspend(reason));
    }

    [Fact]
    public void Reinstate_FromSuspended_SetsPublishedAndClearsSuspensionReason()
    {
        var property = NewPublishedProperty();
        property.Suspend("Reported as a duplicate listing.");

        property.Reinstate();

        Assert.Equal(PropertyStatus.Published, property.Status);
        Assert.Null(property.SuspensionReason);
    }

    [Fact]
    public void Reinstate_WhenNotSuspended_Throws()
    {
        var property = NewPublishedProperty();

        Assert.Throws<InvalidOperationException>(() => property.Reinstate());
    }

    [Fact]
    public void MarkAsRented_FromPublishedRentalListing_SetsRented()
    {
        var property = NewPublishedProperty(ListingType.Rent);

        property.MarkAsRented();

        Assert.Equal(PropertyStatus.Rented, property.Status);
    }

    [Fact]
    public void MarkAsRented_WhenNotPublished_Throws()
    {
        var property = NewProperty(ListingType.Rent);

        Assert.Throws<InvalidOperationException>(() => property.MarkAsRented());
    }

    [Fact]
    public void MarkAsRented_ForSaleListing_Throws()
    {
        var property = NewPublishedProperty(ListingType.Sale);

        Assert.Throws<InvalidOperationException>(() => property.MarkAsRented());
    }

    [Fact]
    public void MarkAsSold_FromPublishedSaleListing_SetsSold()
    {
        var property = NewPublishedProperty(ListingType.Sale);

        property.MarkAsSold();

        Assert.Equal(PropertyStatus.Sold, property.Status);
    }

    [Fact]
    public void MarkAsSold_WhenNotPublished_Throws()
    {
        var property = NewProperty(ListingType.Sale);

        Assert.Throws<InvalidOperationException>(() => property.MarkAsSold());
    }

    [Fact]
    public void MarkAsSold_ForRentalListing_Throws()
    {
        var property = NewPublishedProperty(ListingType.Rent);

        Assert.Throws<InvalidOperationException>(() => property.MarkAsSold());
    }

    [Fact]
    public void Archive_FromPublished_SetsArchivedStatus()
    {
        var property = NewPublishedProperty();

        property.Archive();

        Assert.Equal(PropertyStatus.Archived, property.Status);
    }

    [Fact]
    public void Archive_FromRented_SetsArchivedStatus()
    {
        var property = NewPublishedProperty(ListingType.Rent);
        property.MarkAsRented();

        property.Archive();

        Assert.Equal(PropertyStatus.Archived, property.Status);
    }

    [Fact]
    public void Archive_FromSold_SetsArchivedStatus()
    {
        var property = NewPublishedProperty(ListingType.Sale);
        property.MarkAsSold();

        property.Archive();

        Assert.Equal(PropertyStatus.Archived, property.Status);
    }

    [Fact]
    public void Archive_WhenNotPublishedRentedOrSold_Throws()
    {
        // Archive() now requires the listing to have actually been live — a Draft is discarded
        // via delete, not archived (this changed from earlier behavior; see Archive's own comment).
        var property = NewProperty();

        Assert.Throws<InvalidOperationException>(() => property.Archive());
    }

    [Fact]
    public void Republish_FromArchived_SetsPublishedStatusAndKeepsOriginalPublishedAt()
    {
        var property = NewPublishedProperty();
        var originalPublishedAt = property.PublishedAt;
        property.Archive();

        property.Republish();

        Assert.Equal(PropertyStatus.Published, property.Status);
        Assert.Equal(originalPublishedAt, property.PublishedAt);
    }

    [Fact]
    public void Republish_WhenNotArchived_Throws()
    {
        var property = NewProperty();

        Assert.Throws<InvalidOperationException>(() => property.Republish());
    }
}
