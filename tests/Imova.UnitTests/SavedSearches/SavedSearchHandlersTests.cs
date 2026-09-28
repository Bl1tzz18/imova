using FluentValidation;
using Imova.Application.Features.SavedSearches;
using Imova.Application.Features.SavedSearches.CreateSavedSearch;
using Imova.Application.Features.SavedSearches.ManageSavedSearches;
using Imova.Application.Features.SavedSearches.Unsubscribe;
using Imova.Contracts.SavedSearches;
using Imova.Domain.SavedSearches;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.DataProtection;

namespace Imova.UnitTests.SavedSearches;

public class SavedSearchHandlersTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly Guid _userId = Guid.NewGuid();

    private Task<SavedSearchDto> CreateAsync(string queryString, string name = "Căutare", Guid? userId = null) =>
        new CreateSavedSearchHandler(_db, _clock)
            .Handle(new CreateSavedSearchCommand(userId ?? _userId, name, queryString, AlertFrequency.Daily), CancellationToken.None);

    [Fact]
    public async Task Create_StoresTheSearchNormalized()
    {
        var dto = await CreateAsync("propertyType=Apartment&page=2&transactionType=Rent");

        Assert.Equal("propertyType=Apartment&transactionType=Rent", dto.QueryString);
        Assert.Equal("Daily", dto.AlertFrequency);
        Assert.Single(_db.SavedSearches);
    }

    [Fact]
    public async Task Create_TheSameSearchAgain_UpdatesItInsteadOfAddingADuplicate()
    {
        var first = await CreateAsync("transactionType=Rent&propertyType=Apartment", "Prima");
        var second = await CreateAsync("propertyType=Apartment&transactionType=Rent", "Nou nume");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Nou nume", Assert.Single(_db.SavedSearches).Name);
    }

    [Fact]
    public async Task Create_ASearchTheSearchEndpointWouldReject_IsAValidationError()
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync("minPriceEur=900&maxPriceEur=100"));
        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync("transactionType=Lease"));
        Assert.Empty(_db.SavedSearches);
    }

    [Fact]
    public async Task Create_BeyondTheLimit_IsRefusedWithACode()
    {
        for (var i = 0; i < SavedSearchRules.MaxPerUser; i++)
        {
            await CreateAsync($"minPriceEur={i}");
        }

        var error = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync("minPriceEur=999"));

        Assert.Contains(error.Errors, e => e.ErrorCode == "savedSearch.limitReached");
        // Another user isn't affected.
        await CreateAsync("minPriceEur=999", userId: Guid.NewGuid());
    }

    [Fact]
    public async Task UpdateDeleteAndViewed_OnlyWorkOnTheCallersOwnSearches()
    {
        var mine = await CreateAsync("transactionType=Sale");
        var stranger = Guid.NewGuid();
        var search = new FakeListingSearch([]);

        Assert.Null(await new UpdateSavedSearchHandler(_db, search)
            .Handle(new UpdateSavedSearchCommand(stranger, mine.Id, "X", AlertFrequency.Off), CancellationToken.None));
        Assert.Null(await new MarkSavedSearchViewedHandler(_db, _clock)
            .Handle(new MarkSavedSearchViewedCommand(stranger, mine.Id), CancellationToken.None));
        Assert.False(await new DeleteSavedSearchHandler(_db)
            .Handle(new DeleteSavedSearchCommand(stranger, mine.Id), CancellationToken.None));

        var updated = await new UpdateSavedSearchHandler(_db, search)
            .Handle(new UpdateSavedSearchCommand(_userId, mine.Id, "Case de vânzare", AlertFrequency.Instant), CancellationToken.None);
        Assert.Equal(("Case de vânzare", "Instant"), (updated!.Name, updated.AlertFrequency));
        Assert.True(await new DeleteSavedSearchHandler(_db)
            .Handle(new DeleteSavedSearchCommand(_userId, mine.Id), CancellationToken.None));
        Assert.Empty(_db.SavedSearches);
    }

    [Fact]
    public async Task Get_CountsNewListingsSinceTheLastVisit_AndViewingResetsIt()
    {
        var created = await CreateAsync("transactionType=Sale");
        var search = new FakeListingSearch(q => q.PublishedAfter < _clock.Now ? ([], 3) : ([], 0));
        _clock.Advance(TimeSpan.FromHours(1));

        var list = await new GetSavedSearchesHandler(_db, search).Handle(new GetSavedSearchesQuery(_userId), CancellationToken.None);
        Assert.Equal(3, Assert.Single(list).NewListingsCount);
        Assert.Equal(created.CreatedAt, search.Received.Single().PublishedAfter);

        await new MarkSavedSearchViewedHandler(_db, _clock).Handle(new MarkSavedSearchViewedCommand(_userId, created.Id), CancellationToken.None);
        list = await new GetSavedSearchesHandler(_db, search).Handle(new GetSavedSearchesQuery(_userId), CancellationToken.None);
        Assert.Equal(0, Assert.Single(list).NewListingsCount);
    }

    [Fact]
    public async Task Unsubscribe_WithTheEmailedToken_TurnsAlertsOff_AndRejectsAForgedOne()
    {
        var created = await CreateAsync("transactionType=Sale", "Case");
        var tokens = new SavedSearchUnsubscribeTokens(new EphemeralDataProtectionProvider());
        var handler = new UnsubscribeSavedSearchHandler(_db, tokens);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UnsubscribeSavedSearchCommand(created.Id, tokens.Create(Guid.NewGuid())), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UnsubscribeSavedSearchCommand(created.Id, "forged"), CancellationToken.None));

        var result = await handler.Handle(new UnsubscribeSavedSearchCommand(created.Id, tokens.Create(created.Id)), CancellationToken.None);

        Assert.Equal("Case", result!.Name);
        Assert.Equal(AlertFrequency.Off, Assert.Single(_db.SavedSearches).AlertFrequency);
    }
}
