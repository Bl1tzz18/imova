using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingTests
{
    private static Listing NewRental() => ListingTestData.NewListing(transactionType: TransactionType.Rent);

    private static Listing NewSale() => ListingTestData.NewListing(transactionType: TransactionType.Sale);

    // --- Create / details invariants ---

    [Fact]
    public void Create_StartsAsDraftWithNoPublishedAt()
    {
        var listing = NewRental();

        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Null(listing.PublishedAt);
        Assert.NotEqual(Guid.Empty, listing.Id);
    }

    [Fact]
    public void Create_WithClientSuppliedId_UsesIt()
    {
        var id = Guid.NewGuid();

        var listing = Listing.Create(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Sale, "T", "D", ListingTestData.Eur(1m), id: id);

        Assert.Equal(id, listing.Id);
    }

    [Fact]
    public void Create_SaleListing_GetsEmptySaleDetailsAndNoRentalDetails()
    {
        var listing = NewSale();

        Assert.NotNull(listing.SaleDetails);
        Assert.Null(listing.RentalDetails);
    }

    [Fact]
    public void Create_RentalWithoutRentalDetails_Throws()
    {
        Assert.Throws<ArgumentException>(() => Listing.Create(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Rent, "T", "D", ListingTestData.Eur(1m)));
    }

    [Fact]
    public void Create_SaleWithRentalDetails_Throws()
    {
        Assert.Throws<ArgumentException>(() => Listing.Create(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Sale, "T", "D", ListingTestData.Eur(1m), rentalDetails: new RentalDetails()));
    }

    [Fact]
    public void Create_RentalWithSaleDetails_Throws()
    {
        Assert.Throws<ArgumentException>(() => Listing.Create(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Rent, "T", "D", ListingTestData.Eur(1m),
            saleDetails: new SaleDetails(), rentalDetails: new RentalDetails()));
    }

    [Theory]
    [InlineData("", "D")]
    [InlineData(" ", "D")]
    [InlineData("T", "")]
    public void Create_WithMissingTitleOrDescription_Throws(string title, string description)
    {
        Assert.Throws<ArgumentException>(() => Listing.Create(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Sale, title, description, ListingTestData.Eur(1m)));
    }

    [Fact]
    public void Create_WithEmptyPropertyOrPublisherId_Throws()
    {
        Assert.Throws<ArgumentException>(() => Listing.Create(
            Guid.Empty, Guid.NewGuid(), TransactionType.Sale, "T", "D", ListingTestData.Eur(1m)));
        Assert.Throws<ArgumentException>(() => Listing.Create(
            Guid.NewGuid(), Guid.Empty, TransactionType.Sale, "T", "D", ListingTestData.Eur(1m)));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, -1)]
    public void Create_WithInvalidRentalTerms_Throws(int? minLeaseMonths, int? deposit)
    {
        var details = new RentalDetails(MinLeasePeriodMonths: minLeaseMonths, SecurityDepositAmount: deposit);

        Assert.Throws<ArgumentOutOfRangeException>(() => Listing.Create(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Rent, "T", "D", ListingTestData.Eur(1m), rentalDetails: details));
    }

    [Fact]
    public void UpdateDetails_SwitchingSaleToRent_ReplacesSaleDetailsWithRentalDetails()
    {
        var listing = NewSale();
        var rental = new RentalDetails(PetsAllowed: true);

        listing.UpdateDetails(TransactionType.Rent, "New", "Desc", ListingTestData.Eur(400m), null, rental);

        Assert.Equal(TransactionType.Rent, listing.TransactionType);
        Assert.Null(listing.SaleDetails);
        Assert.Equal(rental, listing.RentalDetails);
        Assert.Equal(400m, listing.Price.Amount);
        Assert.Equal("New", listing.Title);
    }

    [Fact]
    public void UpdateDetails_DoesNotChangeStatus()
    {
        var listing = NewRental().MoveTo(ListingStatus.Active);

        listing.UpdateDetails(TransactionType.Rent, "New", "Desc", ListingTestData.Eur(400m), null, new RentalDetails());

        Assert.Equal(ListingStatus.Active, listing.Status);
    }

    // --- Lifecycle transitions ---

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Rejected)]
    public void SubmitForReview_FromDraftOrRejected_MovesToPendingReviewAndClearsRejectionReason(ListingStatus from)
    {
        var listing = NewRental().MoveTo(from);

        listing.SubmitForReview();

        Assert.Equal(ListingStatus.PendingReview, listing.Status);
        Assert.Null(listing.RejectionReason);
    }

    [Theory]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Archived)]
    public void SubmitForReview_FromOtherStatuses_Throws(ListingStatus from)
    {
        var listing = NewRental().MoveTo(from);

        Assert.Throws<InvalidOperationException>(listing.SubmitForReview);
    }

    [Fact]
    public void Approve_FromPendingReview_ActivatesAndStampsPublishedAt()
    {
        var listing = NewRental().MoveTo(ListingStatus.PendingReview);

        listing.Approve();

        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.NotNull(listing.PublishedAt);
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Rejected)]
    public void Approve_FromOtherStatuses_Throws(ListingStatus from)
    {
        Assert.Throws<InvalidOperationException>(NewRental().MoveTo(from).Approve);
    }

    [Fact]
    public void Reject_FromPendingReview_StoresReason()
    {
        var listing = NewRental().MoveTo(ListingStatus.PendingReview);

        listing.Reject("Poze neclare.");

        Assert.Equal(ListingStatus.Rejected, listing.Status);
        Assert.Equal("Poze neclare.", listing.RejectionReason);
    }

    [Fact]
    public void Reject_WithBlankReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewRental().MoveTo(ListingStatus.PendingReview).Reject(" "));
    }

    [Fact]
    public void Reject_WhenNotPendingReview_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NewRental().MoveTo(ListingStatus.Active).Reject("x"));
    }

    [Fact]
    public void Suspend_ThenReinstate_RoundTripsThroughSuspended()
    {
        var listing = NewRental().MoveTo(ListingStatus.Active);

        listing.Suspend("Conținut duplicat.");
        Assert.Equal(ListingStatus.Suspended, listing.Status);
        Assert.Equal("Conținut duplicat.", listing.SuspensionReason);

        listing.Reinstate();
        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Null(listing.SuspensionReason);
    }

    [Fact]
    public void Suspend_WhenNotActive_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NewRental().MoveTo(ListingStatus.PendingReview).Suspend("x"));
    }

    [Fact]
    public void Reinstate_WhenNotSuspended_Throws()
    {
        Assert.Throws<InvalidOperationException>(NewRental().MoveTo(ListingStatus.Active).Reinstate);
    }

    [Fact]
    public void MarkAsRented_ActiveRental_MovesToRented()
    {
        var listing = NewRental().MoveTo(ListingStatus.Active);

        listing.MarkAsRented();

        Assert.Equal(ListingStatus.Rented, listing.Status);
    }

    [Fact]
    public void MarkAsRented_OnSaleListing_Throws()
    {
        Assert.Throws<InvalidOperationException>(NewSale().MoveTo(ListingStatus.Active).MarkAsRented);
    }

    [Fact]
    public void MarkAsSold_ActiveSale_MovesToSold()
    {
        var listing = NewSale().MoveTo(ListingStatus.Active);

        listing.MarkAsSold();

        Assert.Equal(ListingStatus.Sold, listing.Status);
    }

    [Fact]
    public void MarkAsSold_OnRentalListing_Throws()
    {
        Assert.Throws<InvalidOperationException>(NewRental().MoveTo(ListingStatus.Active).MarkAsSold);
    }

    [Fact]
    public void MarkAsSold_WhenNotActive_Throws()
    {
        Assert.Throws<InvalidOperationException>(NewSale().MoveTo(ListingStatus.PendingReview).MarkAsSold);
    }

    [Fact]
    public void Expire_FromActive_MovesToExpired()
    {
        var listing = NewRental().MoveTo(ListingStatus.Active);

        listing.Expire();

        Assert.Equal(ListingStatus.Expired, listing.Status);
    }

    [Fact]
    public void Expire_WhenNotActive_Throws()
    {
        Assert.Throws<InvalidOperationException>(NewRental().MoveTo(ListingStatus.Draft).Expire);
    }

    [Theory]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Rented)]
    [InlineData(ListingStatus.Expired)]
    public void Archive_FromALiveOrFinishedStatus_Archives(ListingStatus from)
    {
        var listing = NewRental().MoveTo(from);

        listing.Archive();

        Assert.Equal(ListingStatus.Archived, listing.Status);
    }

    [Fact]
    public void Archive_FromSold_Archives()
    {
        var listing = NewSale().MoveTo(ListingStatus.Sold);

        listing.Archive();

        Assert.Equal(ListingStatus.Archived, listing.Status);
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Rejected)]
    [InlineData(ListingStatus.Suspended)]
    [InlineData(ListingStatus.Archived)]
    public void Archive_FromNeverLiveOrAlreadyArchived_Throws(ListingStatus from)
    {
        Assert.Throws<InvalidOperationException>(NewRental().MoveTo(from).Archive);
    }

    [Theory]
    [InlineData(ListingStatus.Archived)]
    [InlineData(ListingStatus.Expired)]
    public void Publish_FromArchivedOrExpired_ReactivatesAndKeepsOriginalPublishedAt(ListingStatus from)
    {
        var listing = NewRental().MoveTo(from);
        var originalPublishedAt = listing.PublishedAt;

        listing.Publish();

        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal(originalPublishedAt, listing.PublishedAt);
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Suspended)]
    public void Publish_FromOtherStatuses_Throws(ListingStatus from)
    {
        Assert.Throws<InvalidOperationException>(NewRental().MoveTo(from).Publish);
    }
}
