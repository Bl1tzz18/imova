using Imova.Application.Features.Amenities.GetAmenities;
using Imova.Infrastructure.Amenities;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.UnitTests.Amenities;

public class GetAmenitiesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTheSeededAmenitiesSortedByLabel()
    {
        await using var dbContext = TestDbContextFactory.Create();
        // EnsureCreated applies AmenityConfiguration's HasData seed, same as the migration does.
        await dbContext.Database.EnsureCreatedAsync();

        var result = await new GetAmenitiesHandler(dbContext, new MemoryCache(new MemoryCacheOptions()))
            .Handle(new GetAmenitiesQuery(), CancellationToken.None);

        Assert.Equal(AmenityConfiguration.Seed.Count, result.Count);
        Assert.Contains(result, a => a.Key == "parking" && a.LabelRo == "Parcare" && a.Category == "Leisure");
        Assert.Contains(result, a => a.Key == "sauna" && a.Category == "Leisure");
        Assert.Equal(result.Select(a => a.LabelRo).Order(StringComparer.Ordinal), result.Select(a => a.LabelRo));
    }

    [Fact]
    public async Task Handle_ServesRepeatCallsFromTheCache()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await dbContext.Database.EnsureCreatedAsync();
        var handler = new GetAmenitiesHandler(dbContext, new MemoryCache(new MemoryCacheOptions()));

        var first = await handler.Handle(new GetAmenitiesQuery(), CancellationToken.None);
        dbContext.Amenities.RemoveRange(dbContext.Amenities);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var second = await handler.Handle(new GetAmenitiesQuery(), CancellationToken.None);

        Assert.Same(first, second);
    }

    [Fact]
    public void Seed_HasUniqueKeysAndIncludesTheRequiredAmenities()
    {
        var keys = AmenityConfiguration.Seed.Select(a => a.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        foreach (var required in new[]
                 {
                     "parking", "balcony", "elevator", "air_conditioning", "furnished", "garage", "yard",
                     "autonomous_heating", "centralized_heating", "wheelchair_access",
                     // House amenities
                     "fireplace", "underfloor_heating", "smart_home", "appliances", "internet", "cable_tv", "landline",
                     "intercom", "alarm_system", "video_surveillance", "remote_gate",
                     "sauna", "basement", "gazebo", "pool", "terrace", "garden", "staff_room", "backup_generator",
                     "water_purification",
                 })
        {
            Assert.Contains(required, keys);
        }

        Assert.Equal(AmenityConfiguration.Seed.Count, AmenityConfiguration.Seed.Select(a => a.Id).Distinct().Count());
    }
}
