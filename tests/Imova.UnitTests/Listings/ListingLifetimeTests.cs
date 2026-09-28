using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

// ExpiresAt / ExpiryReminderSentAt across the lifecycle — see Listing.ActiveMonths.
public class ListingLifetimeTests
{
    private static DateTimeOffset SixMonthsFromNow => DateTimeOffset.UtcNow.AddMonths(Listing.ActiveMonths);

    private static void AssertAboutSixMonthsFromNow(DateTimeOffset? expiresAt)
    {
        Assert.NotNull(expiresAt);
        Assert.InRange(expiresAt.Value, SixMonthsFromNow.AddMinutes(-1), SixMonthsFromNow.AddMinutes(1));
    }

    [Fact]
    public void ActiveMonths_IsSix() => Assert.Equal(6, Listing.ActiveMonths);

    [Fact]
    public void ADraftOrPendingListing_HasNoExpiry()
    {
        Assert.Null(ListingTestData.NewListing().ExpiresAt);
        Assert.Null(ListingTestData.NewListing().MoveTo(ListingStatus.PendingReview).ExpiresAt);
    }

    [Fact]
    public void Approve_StartsASixMonthPeriod()
    {
        AssertAboutSixMonthsFromNow(ListingTestData.NewListing().MoveTo(ListingStatus.Active).ExpiresAt);
    }

    [Theory]
    [InlineData(ListingStatus.Expired)]
    [InlineData(ListingStatus.Archived)]
    public void Publish_StartsAFreshPeriod_AndForgetsTheOldReminder(ListingStatus from)
    {
        var listing = ListingTestData.NewListing().MoveTo(ListingStatus.Active);
        listing.MarkExpiryReminderSent(DateTimeOffset.UtcNow);
        if (from == ListingStatus.Expired)
        {
            listing.Expire();
        }
        else
        {
            listing.Archive();
        }

        listing.Publish();

        AssertAboutSixMonthsFromNow(listing.ExpiresAt);
        Assert.Null(listing.ExpiryReminderSentAt);
    }

    [Fact]
    public void Renew_ExtendsAnActiveListing_AndForgetsTheReminder()
    {
        var listing = ListingTestData.NewListing().MoveTo(ListingStatus.Active);
        listing.MarkExpiryReminderSent(DateTimeOffset.UtcNow);

        listing.Renew();

        Assert.Equal(ListingStatus.Active, listing.Status);
        AssertAboutSixMonthsFromNow(listing.ExpiresAt);
        Assert.Null(listing.ExpiryReminderSentAt);
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Suspended)]
    [InlineData(ListingStatus.Expired)]
    [InlineData(ListingStatus.Archived)]
    public void Renew_OfANonActiveListing_Throws(ListingStatus from)
    {
        Assert.Throws<InvalidOperationException>(ListingTestData.NewListing().MoveTo(from).Renew);
    }

    [Fact]
    public void Reinstate_KeepsAPeriodThatHasNotRunOut()
    {
        var listing = ListingTestData.NewListing().MoveTo(ListingStatus.Suspended);
        var expiresAt = listing.ExpiresAt;

        listing.Reinstate();

        Assert.Equal(expiresAt, listing.ExpiresAt);
    }
}
