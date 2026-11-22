using Imova.Application.Features.Locations.SearchLocations;
using Imova.Domain.Locations;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.UnitTests.Search;

public class SearchLocationsTests
{
    [Theory]
    [InlineData("Râșcani", "rascana")]
    [InlineData("Rîșcani", "rascana")]
    [InlineData("  RISCANI ", "rascana")]
    [InlineData("Ștefan Vodă", "stefan voda")]
    public void Normalize_FoldsCaseAndRomanianDiacritics_LikeTheFrontend(string input, string expected)
    {
        Assert.Equal(expected, LocationSearchText.Normalize(input));
    }

    [Fact]
    public async Task Handle_FindsRaioaneLocalitatiAndSectors_PrefixMatchesFirst()
    {
        var db = TestDbContextFactory.Create();
        var chisinau = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector);
        var ialoveni = Raion.Create(Guid.NewGuid(), "1200", "Ialoveni", null, LocalityLabel.Localitate);
        db.Raioane.AddRange(chisinau, ialoveni);
        db.Localitati.AddRange(
            Localitate.Create(Guid.NewGuid(), ialoveni.Id, null, "1201", "Botanica Nouă", null),
            Localitate.Create(Guid.NewGuid(), ialoveni.Id, null, "1202", "Sat Botanic", null));
        db.ChisinauSectors.Add(ChisinauSector.Create("Botanica"));
        await db.SaveChangesAsync();
        var handler = new SearchLocationsHandler(db, new MemoryCache(new MemoryCacheOptions()));

        var matches = await handler.Handle(new SearchLocationsQuery("botan"), CancellationToken.None);

        Assert.Equal(["Botanica", "Botanica Nouă", "Sat Botanic"], matches.Select(m => m.Name));
        Assert.Equal(("Sector", chisinau.Id, "Chișinău"), (matches[0].Kind, matches[0].RaionId, matches[0].RaionName));
        Assert.Equal(("Localitate", ialoveni.Id), (matches[1].Kind, matches[1].RaionId));
    }

    [Fact]
    public async Task Handle_NeedsAtLeastTwoCharacters_AndCapsTheResults()
    {
        var db = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "1200", "Ialoveni", null, LocalityLabel.Localitate);
        db.Raioane.Add(raion);
        db.Localitati.AddRange(Enumerable.Range(0, 30).Select(i => Localitate.Create(Guid.NewGuid(), raion.Id, null, $"{i}", $"Sat {i}", null)));
        await db.SaveChangesAsync();
        var handler = new SearchLocationsHandler(db, new MemoryCache(new MemoryCacheOptions()));

        Assert.Empty(await handler.Handle(new SearchLocationsQuery("s"), CancellationToken.None));
        Assert.Equal(8, (await handler.Handle(new SearchLocationsQuery("sat"), CancellationToken.None)).Count);
        Assert.Equal(20, (await handler.Handle(new SearchLocationsQuery("sat", Limit: 99), CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Handle_LeavesOutChisinausAdministrativeSectorLocalities_SoASectorIsListedOnce()
    {
        var db = TestDbContextFactory.Create();
        var chisinau = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector);
        db.Raioane.Add(chisinau);
        db.Localitati.AddRange(
            Localitate.Create(Guid.NewGuid(), chisinau.Id, null, "0110", "Sectorul Botanica", null),
            Localitate.Create(Guid.NewGuid(), chisinau.Id, null, "0111", "Băcioi", null));
        db.ChisinauSectors.Add(ChisinauSector.Create("Botanica"));
        await db.SaveChangesAsync();
        var handler = new SearchLocationsHandler(db, new MemoryCache(new MemoryCacheOptions()));

        var botanica = await handler.Handle(new SearchLocationsQuery("bota"), CancellationToken.None);
        var bacioi = await handler.Handle(new SearchLocationsQuery("baci"), CancellationToken.None);

        Assert.Equal([("Sector", "Botanica")], botanica.Select(m => (m.Kind, m.Name)));
        Assert.Equal(["Băcioi"], bacioi.Select(m => m.Name));
    }
}
