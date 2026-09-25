using Imova.Application.Features.Proximities.GetProximities;
using Imova.Domain.Properties;
using Imova.Domain.Proximities;
using Imova.Infrastructure.Amenities;
using Imova.Infrastructure.Proximities;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.UnitTests.Proximities;

public class ProximityTests
{
    [Fact]
    public void Seed_HasTheTenProximitiesWithUniqueKeysAndIds()
    {
        Assert.Equal(
            [
                "kindergarten", "school", "supermarket", "pharmacy", "public_transport", "park", "city_center", "hospital",
                "farmers_market", "bank",
            ],
            ProximityConfiguration.Seed.Select(p => p.Key));
        Assert.Equal(ProximityConfiguration.Seed.Count, ProximityConfiguration.Seed.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void Seed_EveryProximityAppliesToEveryPropertyType()
    {
        Assert.All(
            ProximityConfiguration.Seed,
            p => Assert.All(Enum.GetValues<PropertyType>(), t => Assert.True(p.AppliesTo(t))));
    }

    [Fact]
    public void Seed_DoesNotShareIdsOrKeysWithAmenities()
    {
        Assert.Empty(ProximityConfiguration.Seed.Select(p => p.Id).Intersect(AmenityConfiguration.Seed.Select(a => a.Id)));
        Assert.Empty(ProximityConfiguration.Seed.Select(p => p.Key).Intersect(AmenityConfiguration.Seed.Select(a => a.Key)));
    }

    [Fact]
    public void AProximity_OnlyAppliesToItsPropertyTypes()
    {
        var proximity = new Proximity(Guid.NewGuid(), "ski_lift", "Pârtie", [PropertyType.House]);

        Assert.True(proximity.AppliesTo(PropertyType.House));
        Assert.False(proximity.AppliesTo(PropertyType.Garage));
    }

    [Fact]
    public async Task GetProximities_ReturnsTheSeedInSeedOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        // EnsureCreated applies ProximityConfiguration's HasData seed, same as the migration does.
        await dbContext.Database.EnsureCreatedAsync();

        var result = await new GetProximitiesHandler(dbContext, new MemoryCache(new MemoryCacheOptions()))
            .Handle(new GetProximitiesQuery(), CancellationToken.None);

        Assert.Equal(ProximityConfiguration.Seed.Select(p => p.Key), result.Select(p => p.Key));
        Assert.Contains(result, p => p.Key == "pharmacy" && p.LabelRo == "Farmacie" && p.ApplicablePropertyTypes.Count == 6);
    }

    [Fact]
    public async Task GetProximities_ServesRepeatCallsFromTheCache()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await dbContext.Database.EnsureCreatedAsync();
        var handler = new GetProximitiesHandler(dbContext, new MemoryCache(new MemoryCacheOptions()));

        var first = await handler.Handle(new GetProximitiesQuery(), CancellationToken.None);
        dbContext.Proximities.RemoveRange(dbContext.Proximities);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var second = await handler.Handle(new GetProximitiesQuery(), CancellationToken.None);

        Assert.Same(first, second);
    }
}
