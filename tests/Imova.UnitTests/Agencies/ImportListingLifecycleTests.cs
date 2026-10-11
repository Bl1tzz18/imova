using Imova.Application.Features.Agencies.Import;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Agencies;

// What an import does to a listing's lifecycle: a new one goes live (or starts deactivated) without
// review; an existing one follows the row's status but never overrides moderation or a closed deal.
public class ImportListingLifecycleTests
{
    // A sale, so it can also be marked sold (a rental is marked rented).
    private static Listing AgencyListing(TransactionType transactionType = TransactionType.Sale) =>
        ListingTestData.NewListing(transactionType: transactionType, agencyId: Guid.NewGuid());

    [Fact]
    public void ANewImportedListing_GoesLiveWithoutReview()
    {
        var listing = AgencyListing();
        listing.AssignExternalRef("  AP-1024 ");
        listing.PublishImported();

        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal("AP-1024", listing.ExternalRef);
        Assert.NotNull(listing.PublishedAt);
        Assert.NotNull(listing.ExpiresAt);
    }

    [Fact]
    public void ANewInactiveRow_StartsDeactivated_NeverPublished()
    {
        var listing = AgencyListing();
        listing.ImportDeactivated();

        Assert.Equal(ListingStatus.Archived, listing.Status);
        Assert.Null(listing.PublishedAt);

        listing.Publish();
        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.NotNull(listing.PublishedAt);
    }

    [Fact]
    public void OnlyAnAgencysDraft_CanBeImported_AndOnlyAnAgencysListing_HasAnExternalRef()
    {
        Assert.Throws<InvalidOperationException>(() => ListingTestData.NewListing().AssignExternalRef("X"));
        Assert.Throws<ArgumentException>(() => AgencyListing().AssignExternalRef(new string('x', 101)));
        Assert.Throws<InvalidOperationException>(() => AgencyListing().MoveTo(ListingStatus.PendingReview).PublishImported());
        Assert.Throws<InvalidOperationException>(() => AgencyListing().MoveTo(ListingStatus.Active).ImportDeactivated());
    }

    [Theory]
    [InlineData(ListingStatus.Active, true, ListingStatus.Archived)]
    [InlineData(ListingStatus.Active, false, ListingStatus.Active)]
    [InlineData(ListingStatus.Archived, false, ListingStatus.Active)]
    [InlineData(ListingStatus.Expired, false, ListingStatus.Active)]
    [InlineData(ListingStatus.Archived, true, ListingStatus.Archived)]
    [InlineData(ListingStatus.Draft, false, ListingStatus.Active)]
    [InlineData(ListingStatus.Draft, true, ListingStatus.Archived)]
    [InlineData(ListingStatus.PendingReview, false, ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Rejected, false, ListingStatus.Rejected)]
    [InlineData(ListingStatus.Suspended, false, ListingStatus.Suspended)]
    [InlineData(ListingStatus.Suspended, true, ListingStatus.Suspended)]
    [InlineData(ListingStatus.Sold, false, ListingStatus.Sold)]
    [InlineData(ListingStatus.Rented, true, ListingStatus.Rented)]
    public void AReImport_FollowsTheRowsStatus_ButNeverOverridesModerationOrAClosedDeal(ListingStatus from, bool inactive, ListingStatus expected)
    {
        var listing = AgencyListing(from == ListingStatus.Rented ? TransactionType.Rent : TransactionType.Sale).MoveTo(from);

        ImportListingWriter.ApplyStatus(listing, inactive);

        Assert.Equal(expected, listing.Status);
    }
}
