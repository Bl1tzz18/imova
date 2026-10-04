using Imova.Application.Common;
using Imova.Application.Features.Favorites.Alerts;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Publishers;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Favorites;

// The saved-listing alert job: one email per price change (at most one a day per listing per user,
// batched to the latest price), one when the listing ends and then nothing more.
public class FavoriteAlertsTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeEmailSender _email = new();
    private readonly FavoriteAlertUnsubscribeTokens _tokens = new(new EphemeralDataProtectionProvider());
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _visitorId = Guid.NewGuid();
    private readonly Publisher _publisher;
    private readonly Listing _listing;

    public FavoriteAlertsTests()
    {
        ListingTestData.AddUser(_db, _sellerId);
        ListingTestData.AddUser(_db, _visitorId);
        _publisher = ListingTestData.AddIndividualPublisher(_db, _sellerId);
        _listing = ListingTestData.AddListing(_db, _publisher.Id, TransactionType.Sale, ListingTestData.Eur(80_000m)).MoveTo(ListingStatus.Active);
        _db.SaveChanges();
    }

    private Task<int> RunAsync() => new FavoriteAlerts(
            _db, new FakeBlobStorageService(), _email, new AppOptions { WebBaseUrl = "https://imova.test" }, _tokens, _clock, NullLogger<FavoriteAlerts>.Instance)
        .RunAsync(CancellationToken.None);

    private Favorite Save(Guid? userId = null, Listing? listing = null)
    {
        listing ??= _listing;
        var favorite = Favorite.Create(userId ?? _visitorId, listing.Id, listing.Price, now: _clock.Now);
        _db.Favorites.Add(favorite);
        _db.SaveChanges();
        return favorite;
    }

    private void ChangePrice(decimal amount, Listing? listing = null)
    {
        listing ??= _listing;
        listing.UpdateDetails(
            listing.TransactionType, listing.Title, listing.Description, ListingTestData.Eur(amount), listing.SaleDetails, listing.RentalDetails);
        _db.SaveChanges();
    }

    private Favorite Reload(Favorite favorite) => _db.Favorites.AsNoTracking().Single(f => f.Id == favorite.Id);

    [Fact]
    public async Task NoChange_SendsNothing()
    {
        Save();

        Assert.Equal(0, await RunAsync());
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task PriceChange_EmailsOldAndNewPrice_WithTheListingLink_Once()
    {
        Save();
        ChangePrice(76_000m);

        Assert.Equal(1, await RunAsync());

        var email = Assert.Single(_email.Sent);
        Assert.Equal($"{_visitorId:N}@example.com", email.To);
        Assert.Contains("Prețul a scăzut", email.Subject);
        Assert.Contains("Preț vechi: 80 000 EUR", email.TextBody);
        Assert.Contains("Preț nou: 76 000 EUR (−5%)", email.TextBody);
        Assert.Contains($"https://imova.test/property/{_listing.Id}", email.TextBody);
        Assert.Contains($"https://imova.test/favorites/unsubscribe?user={_visitorId}&token=", email.TextBody);
        Assert.Contains("https://imova.test/account?tab=notifications", email.TextBody);
        Assert.Contains("80 000 EUR", email.HtmlBody);
        Assert.Contains("76 000 EUR", email.HtmlBody);

        // Nothing new to say on the next run.
        _clock.Advance(TimeSpan.FromDays(2));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task PriceEmail_ShowsTheListingAsACard_WithItsMainPhotoAndPlace()
    {
        _db.Photos.Add(Photo.Create(_listing.Id, "second.jpg", "image/jpeg", 10, sortOrder: 1));
        _db.Photos.Add(Photo.Create(_listing.Id, "cover.jpg", "image/jpeg", 10, sortOrder: 0, isPrimary: true));
        Save();
        ChangePrice(76_000m);

        await RunAsync();

        var html = Assert.Single(_email.Sent).HtmlBody!;
        Assert.Contains("<img src=\"https://blob.test/cover.jpg\"", html);
        Assert.DoesNotContain("second.jpg", html);
        Assert.Contains("Apartament · Vânzare", html);
        Assert.Contains($"href=\"https://imova.test/property/{_listing.Id}\"", html);
        Assert.Contains("line-through;white-space:nowrap;\">80 000 EUR", html);
        Assert.Contains("−5%", html);
    }

    [Fact]
    public async Task EndedEmail_CardHasTheStatusBadge_AndNoImageWithoutPhotos()
    {
        Save();
        _listing.MarkAsSold();
        _db.SaveChanges();

        await RunAsync();

        var html = Assert.Single(_email.Sent).HtmlBody!;
        Assert.Contains(">Vândut</span>", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("80 000 EUR", html);
    }

    [Fact]
    public async Task SeveralChangesInADay_OneEmail_ThenOneWithTheLatestPrice()
    {
        Save();
        ChangePrice(78_000m);
        await RunAsync();

        // Two more changes the same day: no email yet.
        _clock.Advance(TimeSpan.FromHours(2));
        ChangePrice(77_000m);
        Assert.Equal(0, await RunAsync());
        _clock.Advance(TimeSpan.FromHours(3));
        ChangePrice(75_000m);
        Assert.Equal(0, await RunAsync());
        Assert.Single(_email.Sent);

        // A day after the first email: one email, from the price they were told to the latest one.
        _clock.Advance(TimeSpan.FromHours(19));
        Assert.Equal(1, await RunAsync());

        Assert.Equal(2, _email.Sent.Count);
        Assert.Contains("Preț vechi: 78 000 EUR", _email.Sent[1].TextBody);
        Assert.Contains("Preț nou: 75 000 EUR", _email.Sent[1].TextBody);
        Assert.DoesNotContain("77 000", _email.Sent[1].TextBody);
    }

    [Fact]
    public async Task PriceBackWhereItWas_SendsNothing()
    {
        Save();
        ChangePrice(70_000m);
        ChangePrice(80_000m);

        Assert.Equal(0, await RunAsync());
    }

    [Fact]
    public async Task PriceRaise_IsReportedToo()
    {
        Save();
        ChangePrice(84_000m);

        await RunAsync();

        var email = Assert.Single(_email.Sent);
        Assert.Contains("Prețul s-a schimbat", email.Subject);
        Assert.Contains("Preț nou: 84 000 EUR (+5%)", email.TextBody);
    }

    [Fact]
    public async Task OneEmailPerUser_ForTheSameListing()
    {
        var other = Guid.NewGuid();
        ListingTestData.AddUser(_db, other);
        Save();
        Save(other);
        ChangePrice(70_000m);

        Assert.Equal(2, await RunAsync());
        Assert.Equal(
            new[] { $"{_visitorId:N}@example.com", $"{other:N}@example.com" }.Order(),
            _email.Sent.Select(e => e.To).Order());
    }

    [Theory]
    [InlineData(ListingStatus.Sold, "Proprietatea a fost vândută.")]
    [InlineData(ListingStatus.Expired, "Anunțul a expirat și nu a fost prelungit.")]
    [InlineData(ListingStatus.Archived, "Autorul a retras anunțul.")]
    public async Task Ended_OneEmailWithTheSummaryAndSimilarListings(ListingStatus status, string reason)
    {
        var favorite = Save();
        switch (status)
        {
            case ListingStatus.Sold: _listing.MarkAsSold(); break;
            case ListingStatus.Expired: _listing.Expire(); break;
            default: _listing.Archive(); break;
        }

        _db.SaveChanges();

        Assert.Equal(1, await RunAsync());

        var email = Assert.Single(_email.Sent);
        Assert.Contains("Nu mai este disponibil", email.Subject);
        Assert.Contains(reason, email.TextBody);
        Assert.Contains(_listing.Title, email.TextBody);
        Assert.Contains("Apartament · Vânzare", email.TextBody);
        Assert.Contains("Ultimul preț: 80 000 EUR", email.TextBody);
        Assert.Contains($"https://imova.test/property/{_listing.Id}#similar-listings-title", email.TextBody);
        Assert.Contains("https://imova.test/search?transactionType=Sale&propertyType=Apartment&raionId=", email.TextBody);
        Assert.NotNull(Reload(favorite).EndedAlertSentAt);

        Assert.Equal(0, await RunAsync());
    }

    [Fact]
    public async Task RentedListing_IsEndedToo()
    {
        var rental = ListingTestData.AddListing(_db, _publisher.Id, TransactionType.Rent, ListingTestData.Eur(450m)).MoveTo(ListingStatus.Active);
        _db.SaveChanges();
        Save(listing: rental);
        rental.MarkAsRented();
        _db.SaveChanges();

        await RunAsync();

        Assert.Contains("Proprietatea a fost închiriată.", Assert.Single(_email.Sent).TextBody);
    }

    [Fact]
    public async Task AfterTheEndedEmail_NoMorePriceEmails()
    {
        Save();
        ChangePrice(70_000m);
        _listing.Archive();
        _db.SaveChanges();

        // Ended wins over the pending price change: one email, the "no longer available" one.
        Assert.Equal(1, await RunAsync());
        Assert.Contains("Nu mai este disponibil", Assert.Single(_email.Sent).Subject);

        // Even if it comes back with another price, that favorite stays quiet.
        _listing.Publish();
        _db.SaveChanges();
        ChangePrice(60_000m);
        _clock.Advance(TimeSpan.FromDays(2));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task ListingNotPublicRightNow_WaitsWithThePriceEmail()
    {
        Save();
        ChangePrice(70_000m);
        _listing.Suspend("Verificare.");
        _db.SaveChanges();

        Assert.Equal(0, await RunAsync());

        _listing.Reinstate();
        _db.SaveChanges();
        Assert.Equal(1, await RunAsync());
    }

    [Fact]
    public async Task OptedOut_NoEmail_AndTurningItBackOnDoesNotSendOldNews()
    {
        var user = _db.Users.Single(u => u.Id == _visitorId);
        user.EmailFavoriteUpdates = false;
        var favorite = Save();
        ChangePrice(70_000m);

        Assert.Equal(0, await RunAsync());
        Assert.Empty(_email.Sent);
        Assert.Equal(70_000m, Reload(favorite).KnownPriceAmount);

        user.EmailFavoriteUpdates = true;
        _db.SaveChanges();
        Assert.Equal(0, await RunAsync());

        // A new change after turning it back on is reported, from the price at that time.
        ChangePrice(65_000m);
        Assert.Equal(1, await RunAsync());
        Assert.Contains("Preț vechi: 70 000 EUR", Assert.Single(_email.Sent).TextBody);
    }

    [Fact]
    public async Task OptedOut_EndedListing_IsClosedSilently()
    {
        _db.Users.Single(u => u.Id == _visitorId).EmailFavoriteUpdates = false;
        var favorite = Save();
        _listing.MarkAsSold();
        _db.SaveChanges();

        Assert.Equal(0, await RunAsync());
        Assert.NotNull(Reload(favorite).EndedAlertSentAt);
    }

    [Fact]
    public async Task UnconfirmedEmail_GetsNothing()
    {
        var unconfirmed = Guid.NewGuid();
        ListingTestData.AddUser(_db, unconfirmed, emailConfirmed: false);
        Save(unconfirmed);
        ChangePrice(70_000m);

        Assert.Equal(0, await RunAsync());
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task TheOwnerSavingTheirOwnListing_GetsNothing()
    {
        Save(_sellerId);
        ChangePrice(70_000m);

        Assert.Equal(0, await RunAsync());
    }

    [Fact]
    public async Task FailedSend_IsRetriedNextRun()
    {
        var favorite = Save();
        ChangePrice(70_000m);
        _email.Fail = true;

        Assert.Equal(0, await RunAsync());
        Assert.Equal(80_000m, Reload(favorite).KnownPriceAmount);

        _email.Fail = false;
        Assert.Equal(1, await RunAsync());
        Assert.Contains("Preț vechi: 80 000 EUR", Assert.Single(_email.Sent).TextBody);
    }

    [Fact]
    public async Task FavoriteWithoutAKnownPrice_LearnsTodaysPriceSilently()
    {
        var favorite = Favorite.Create(_visitorId, _listing.Id);
        _db.Favorites.Add(favorite);
        _db.SaveChanges();

        Assert.Equal(0, await RunAsync());
        Assert.Equal(80_000m, Reload(favorite).KnownPriceAmount);
    }

    [Fact]
    public async Task UnsubscribeLink_TokenWorksForThatUserOnly()
    {
        Save();
        ChangePrice(70_000m);
        await RunAsync();

        var text = Assert.Single(_email.Sent).TextBody;
        var token = Uri.UnescapeDataString(text.Split("token=")[1].Split('\n')[0].Trim());
        Assert.True(_tokens.IsValid(_visitorId, token));
        Assert.False(_tokens.IsValid(_sellerId, token));
    }
}
