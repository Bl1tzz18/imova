using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Locations.GetStreetSuggestions;
using Imova.Domain.Locations;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.UnitTests.Locations;

public class GetStreetSuggestionsHandlerTests
{
    private static GetStreetSuggestionsHandler CreateHandler(IApplicationDbContext dbContext, FakeStreetSuggestionService service) =>
        new(dbContext, service, new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task Handle_MapsServiceResultsToDtos()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService
        {
            ResultToReturn = [new StreetSuggestion("Strada Ismail", 47.02, 28.83), new StreetSuggestion("Strada Ismail 2", null, null)],
        };
        var handler = CreateHandler(dbContext, service);

        var result = await handler.Handle(new GetStreetSuggestionsQuery("Ismail", null, null), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Strada Ismail", result[0].Name);
        Assert.Equal(47.02, result[0].Latitude);
        Assert.Equal(28.83, result[0].Longitude);
        Assert.Null(result[1].Latitude);
    }

    [Fact]
    public async Task Handle_WithNoRaionOrLocalitateId_PassesNullLocalityToService()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", null, null), CancellationToken.None);

        Assert.Equal("Ismail", service.LastQueryRequested);
        Assert.Null(service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_WithKnownLocalitateId_ResolvesItsNameAsLocalityBias()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector);
        dbContext.Raioane.Add(raion);
        var localitate = Localitate.Create(Guid.NewGuid(), raion.Id, null, "0101", "Sectorul Botanica", null);
        dbContext.Localitati.Add(localitate);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", raion.Id, localitate.Id), CancellationToken.None);

        Assert.Equal("Sectorul Botanica", service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_WithUnknownLocalitateId_PassesNullLocalityToService()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", null, Guid.NewGuid()), CancellationToken.None);

        Assert.Null(service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_WithRaionOnly_AndASameNamedLocalitate_BiasesByTheTownNotJustTheRaion()
    {
        // Regression coverage for the "Bulevardul Ștefan cel Mare din Chișinău" bug: selecting
        // Raion "Soroca" and leaving Localitate empty must not fall through to an unbiased
        // national search — it should silently resolve Soroca's same-named town internally.
        await using var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "5401", "Soroca", null, LocalityLabel.Localitate);
        dbContext.Raioane.Add(raion);
        dbContext.Localitati.Add(Localitate.Create(Guid.NewGuid(), raion.Id, null, "5402", "Soroca", null));
        dbContext.Localitati.Add(Localitate.Create(Guid.NewGuid(), raion.Id, null, "5403", "Bulboci", null));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Stefan cel Mare", raion.Id, null), CancellationToken.None);

        Assert.Equal("Soroca", service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_WithRaionOnly_AndNoSameNamedLocalitate_FallsBackToTheRaionName()
    {
        // Municipii like Bălți/Bender have no same-named child Localitate (the municipiu itself
        // IS the town) — the bias must still fall back to the Raion's own name rather than no
        // bias at all.
        await using var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "0200", "Bălți", null, LocalityLabel.Localitate);
        dbContext.Raioane.Add(raion);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Stefan cel Mare", raion.Id, null), CancellationToken.None);

        Assert.Equal("Bălți", service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_WithUnknownRaionId_PassesNullLocalityToService()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", Guid.NewGuid(), null), CancellationToken.None);

        Assert.Null(service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_WithBothRaionAndLocalitateId_PrefersTheLocalitateBias()
    {
        // LocalitateId, when present, is the more precise signal and must win over the Raion
        // fallback — e.g. a Chișinău suburb whose name differs from "Chișinău" itself.
        await using var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector);
        dbContext.Raioane.Add(raion);
        var localitate = Localitate.Create(Guid.NewGuid(), raion.Id, null, "0110", "Durlești", null);
        dbContext.Localitati.Add(localitate);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var service = new FakeStreetSuggestionService();
        var handler = CreateHandler(dbContext, service);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", raion.Id, localitate.Id), CancellationToken.None);

        Assert.Equal("Durlești", service.LastLocalityRequested);
    }

    [Fact]
    public async Task Handle_SecondCallForTheSameTuple_DoesNotReQueryTheServiceOrDatabase()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService
        {
            ResultToReturn = [new StreetSuggestion("Strada Ismail", 47.02, 28.83)],
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetStreetSuggestionsHandler(dbContext, service, cache);
        var first = await handler.Handle(new GetStreetSuggestionsQuery("Ismail", null, null), CancellationToken.None);

        // Dispose the context to prove the second call can't be hitting the database or the
        // (already-disposed-dbContext-dependent) service either — if it tried, EF Core would throw
        // ObjectDisposedException instead of returning the cached list.
        await dbContext.DisposeAsync();
        var second = await handler.Handle(new GetStreetSuggestionsQuery("Ismail", null, null), CancellationToken.None);

        Assert.Equal(1, service.CallCount);
        Assert.Equal(first[0].Name, second[0].Name);
    }

    [Fact]
    public async Task Handle_CacheKeyIsCaseAndWhitespaceInsensitiveOnTheQuery()
    {
        // Maximizes cache hits for the backspace-then-retype and stray-whitespace cases this
        // cache exists for — " Ismail " and "ISMAIL" should hit the same entry as "Ismail".
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService
        {
            ResultToReturn = [new StreetSuggestion("Strada Ismail", 47.02, 28.83)],
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetStreetSuggestionsHandler(dbContext, service, cache);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", null, null), CancellationToken.None);
        await handler.Handle(new GetStreetSuggestionsQuery("ISMAIL", null, null), CancellationToken.None);
        await handler.Handle(new GetStreetSuggestionsQuery(" ismail ", null, null), CancellationToken.None);

        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task Handle_DifferentRaionIds_AreNotConflatedInTheCache()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new FakeStreetSuggestionService();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetStreetSuggestionsHandler(dbContext, service, cache);

        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", Guid.NewGuid(), null), CancellationToken.None);
        await handler.Handle(new GetStreetSuggestionsQuery("Ismail", Guid.NewGuid(), null), CancellationToken.None);

        Assert.Equal(2, service.CallCount);
    }
}
