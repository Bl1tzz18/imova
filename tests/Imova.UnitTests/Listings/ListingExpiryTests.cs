using Imova.Application.Common;
using Imova.Application.Features.Listings.Expiry;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Listings;

public class ListingExpiryTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();

    // Listing.Approve stamps ExpiresAt from the real clock, so the job's clock starts there too.
    private readonly ManualTimeProvider _clock = new(DateTimeOffset.UtcNow);
    private readonly FakeEmailSender _email = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public ListingExpiryTests()
    {
        ListingTestData.AddUser(_db, _ownerId);
        _publisherId = ListingTestData.AddIndividualPublisher(_db, _ownerId).Id;
        _db.SaveChanges();
    }

    private Listing Seed(ListingStatus status = ListingStatus.Active)
    {
        var listing = ListingTestData.AddListing(_db, _publisherId).MoveTo(status);
        _db.SaveChanges();
        return listing;
    }

    private Task<int> RunAsync() => new ListingExpiry(
            _db, _email, new AppOptions { WebBaseUrl = "https://imova.test" }, _clock, NullLogger<ListingExpiry>.Instance)
        .RunAsync(CancellationToken.None);

    private async Task<Listing> ReloadAsync(Guid id) => await _db.Listings.AsNoTracking().SingleAsync(l => l.Id == id);

    [Fact]
    public async Task AFreshListing_IsLeftAlone()
    {
        var listing = Seed();

        Assert.Equal(0, await RunAsync());

        Assert.Empty(_email.Sent);
        Assert.Equal(ListingStatus.Active, (await ReloadAsync(listing.Id)).Status);
    }

    [Fact]
    public async Task AWeekBeforeTheEnd_TheOwnerIsRemindedOnce()
    {
        var listing = Seed();
        _clock.Now = listing.ExpiresAt!.Value - TimeSpan.FromDays(3);

        Assert.Equal(1, await RunAsync());
        _clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(0, await RunAsync());

        var email = Assert.Single(_email.Sent);
        Assert.Equal($"{_ownerId:N}@example.com", email.To);
        Assert.Contains(ListingExpiryEmails.FormatDate(listing.ExpiresAt.Value), email.Subject);
        Assert.Contains("https://imova.test/my-listings", email.TextBody);
        Assert.Equal(ListingStatus.Active, (await ReloadAsync(listing.Id)).Status);
    }

    [Fact]
    public async Task EightDaysBeforeTheEnd_ItIsTooEarlyForAReminder()
    {
        var listing = Seed();
        _clock.Now = listing.ExpiresAt!.Value - TimeSpan.FromDays(8);

        Assert.Equal(0, await RunAsync());
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task AFailedReminder_IsRetriedOnTheNextRun()
    {
        var listing = Seed();
        _clock.Now = listing.ExpiresAt!.Value - TimeSpan.FromDays(3);
        _email.Fail = true;

        Assert.Equal(0, await RunAsync());
        Assert.Null((await ReloadAsync(listing.Id)).ExpiryReminderSentAt);

        _email.Fail = false;
        Assert.Equal(1, await RunAsync());
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task OnceThePeriodHasPassed_TheListingExpires_AndTheOwnerIsTold()
    {
        var listing = Seed();
        _clock.Now = listing.ExpiresAt!.Value + TimeSpan.FromMinutes(1);

        Assert.Equal(1, await RunAsync());

        Assert.Equal(ListingStatus.Expired, (await ReloadAsync(listing.Id)).Status);
        var email = Assert.Single(_email.Sent);
        Assert.Equal("Anunțul tău a expirat — IMOVA", email.Subject);

        // Nothing more to do on the next run.
        Assert.Equal(0, await RunAsync());
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task AnExpiryEmailThatFails_StillExpiresTheListing()
    {
        var listing = Seed();
        _clock.Now = listing.ExpiresAt!.Value + TimeSpan.FromMinutes(1);
        _email.Fail = true;

        await RunAsync();

        Assert.Equal(ListingStatus.Expired, (await ReloadAsync(listing.Id)).Status);
    }

    [Fact]
    public async Task ARenewedListing_StartsOver_WithItsOwnReminder()
    {
        var listing = Seed();
        _clock.Now = listing.ExpiresAt!.Value - TimeSpan.FromDays(3);
        await RunAsync();

        listing.Renew();
        await _db.SaveChangesAsync();
        Assert.Null((await ReloadAsync(listing.Id)).ExpiryReminderSentAt);

        // Near the end of the new period it's reminded again.
        _clock.Now = listing.ExpiresAt!.Value - TimeSpan.FromDays(3);
        Assert.Equal(1, await RunAsync());
        Assert.Equal(2, _email.Sent.Count);
        Assert.Equal(ListingStatus.Active, (await ReloadAsync(listing.Id)).Status);
    }

    [Theory]
    [InlineData(ListingStatus.Suspended)]
    [InlineData(ListingStatus.Archived)]
    [InlineData(ListingStatus.Rented)]
    public async Task ListingsThatAreNotActive_AreNeverExpiredOrReminded(ListingStatus status)
    {
        var listing = Seed(status);
        _clock.Now = DateTimeOffset.UtcNow.AddYears(1);

        Assert.Equal(0, await RunAsync());

        Assert.Equal(status, (await ReloadAsync(listing.Id)).Status);
        Assert.Empty(_email.Sent);
    }
}
