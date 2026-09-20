using Imova.Domain.Locations;
using Imova.Infrastructure.Locations;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Locations;

// Exercises the real embedded cuatm-locations.json (1721 records) rather than a synthetic
// fixture — it's fully deterministic and this is the only way to validate the seeder against
// the actual production data shape it will run against at startup.
public class CuatmLocationSeederTests
{
    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_Creates37RaioaneAnd1684Localitati()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);

        Assert.Equal(37, dbContext.Raioane.Count());
        Assert.Equal(1684, dbContext.Localitati.Count());
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);
        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);

        Assert.Equal(37, dbContext.Raioane.Count());
        Assert.Equal(1684, dbContext.Localitati.Count());
    }

    [Fact]
    public async Task SeedAsync_LabelsChisinauAsSectorAndEveryOtherRaionAsLocalitate()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);

        var chisinau = dbContext.Raioane.Single(r => r.NameRo == "Chișinău");
        Assert.Equal(LocalityLabel.Sector, chisinau.LocalityLabel);

        Assert.All(
            dbContext.Raioane.Where(r => r.NameRo != "Chișinău"),
            r => Assert.Equal(LocalityLabel.Localitate, r.LocalityLabel));
    }

    [Fact]
    public async Task SeedAsync_FlattensEveryDepthIntoTheOwningRaion()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);

        var chisinau = dbContext.Raioane.Single(r => r.NameRo == "Chișinău");
        // "Sîngera" sits two CUATM levels below Chișinău (Chișinău -> Sectorul Botanica ->
        // Sîngera) but must still be flattened directly under Chișinău's RaionId.
        var singera = dbContext.Localitati.Single(l => l.NameRo == "Sîngera");
        Assert.Equal(chisinau.Id, singera.RaionId);
    }

    [Fact]
    public async Task SeedAsync_KeepsTheTrueImmediateParentInParentLocalityId()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);

        var sectorulBotanica = dbContext.Localitati.Single(l => l.NameRo == "Sectorul Botanica");
        var singera = dbContext.Localitati.Single(l => l.NameRo == "Sîngera");

        Assert.Equal(sectorulBotanica.Id, singera.ParentLocalityId);
        Assert.Null(sectorulBotanica.ParentLocalityId);
    }
}
