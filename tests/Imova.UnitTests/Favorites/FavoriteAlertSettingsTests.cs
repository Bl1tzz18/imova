using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Account.EmailPreferences;
using Imova.Application.Features.Favorites.Alerts;
using Imova.Application.Features.Favorites.SaveFavorite;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.DataProtection;

namespace Imova.UnitTests.Favorites;

// Turning the saved-listing emails off (settings or the link in an email), and what saving a
// favorite records for them.
public class FavoriteAlertSettingsTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FavoriteAlertUnsubscribeTokens _tokens = new(new EphemeralDataProtectionProvider());
    private readonly Guid _userId = Guid.NewGuid();

    public FavoriteAlertSettingsTests()
    {
        ListingTestData.AddUser(_db, _userId);
        _db.SaveChanges();
    }

    private bool StoredPreference() => _db.Users.Single(u => u.Id == _userId).EmailFavoriteUpdates;

    [Fact]
    public async Task OnByDefault_AndChangeableFromTheSettings()
    {
        Assert.True((await new GetEmailPreferencesHandler(_db).Handle(new GetEmailPreferencesQuery(_userId), CancellationToken.None)).FavoriteUpdates);

        var updated = await new UpdateEmailPreferencesHandler(_db)
            .Handle(new UpdateEmailPreferencesCommand(_userId, FavoriteUpdates: false), CancellationToken.None);

        Assert.False(updated.FavoriteUpdates);
        Assert.False(StoredPreference());
    }

    [Fact]
    public async Task Preferences_OfAGoneAccount_Are401()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            new GetEmailPreferencesHandler(_db).Handle(new GetEmailPreferencesQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribeLink_TurnsTheEmailsOff()
    {
        var done = await new UnsubscribeFavoriteAlertsHandler(_db, _tokens)
            .Handle(new UnsubscribeFavoriteAlertsCommand(_userId, _tokens.Create(_userId)), CancellationToken.None);

        Assert.True(done);
        Assert.False(StoredPreference());
    }

    [Fact]
    public async Task UnsubscribeLink_WithAnotherUsersToken_IsRejected()
    {
        var handler = new UnsubscribeFavoriteAlertsHandler(_db, _tokens);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UnsubscribeFavoriteAlertsCommand(_userId, _tokens.Create(Guid.NewGuid())), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UnsubscribeFavoriteAlertsCommand(_userId, "not-a-token"), CancellationToken.None));
        Assert.True(StoredPreference());
    }

    [Fact]
    public async Task UnsubscribeLink_ForADeletedAccount_IsFalse()
    {
        var gone = Guid.NewGuid();

        Assert.False(await new UnsubscribeFavoriteAlertsHandler(_db, _tokens)
            .Handle(new UnsubscribeFavoriteAlertsCommand(gone, _tokens.Create(gone)), CancellationToken.None));
    }

    [Fact]
    public async Task SavingAFavorite_RemembersThePrice_AndAnEndedListingSendsNothing()
    {
        var publisher = ListingTestData.AddIndividualPublisher(_db, Guid.NewGuid());
        var active = ListingTestData.AddListing(_db, publisher.Id, price: ListingTestData.Eur(450m)).MoveTo(ListingStatus.Active);
        var rented = ListingTestData.AddListing(_db, publisher.Id).MoveTo(ListingStatus.Rented);
        await _db.SaveChangesAsync();
        var handler = new SaveFavoriteHandler(_db);

        await handler.Handle(new SaveFavoriteCommand(_userId, active.Id), CancellationToken.None);
        await handler.Handle(new SaveFavoriteCommand(_userId, rented.Id), CancellationToken.None);

        var activeFavorite = _db.Favorites.Single(f => f.ListingId == active.Id);
        Assert.Equal(450m, activeFavorite.KnownPriceAmount);
        Assert.Equal(Currency.EUR, activeFavorite.KnownPriceCurrency);
        Assert.False(activeFavorite.AlertsStopped);
        Assert.True(_db.Favorites.Single(f => f.ListingId == rented.Id).AlertsStopped);
    }
}
