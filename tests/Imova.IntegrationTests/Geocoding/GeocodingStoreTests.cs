using Imova.Infrastructure.Geocoding;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Imova.IntegrationTests.Geocoding;

// The shared geocoding limit and cache on real Postgres: slots reserved at the same moment from
// separate connections (as the API and the Worker would) come out spaced, and answers are upserted.
public class GeocodingStoreTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    [Fact]
    public async Task SlotsReservedAtOnce_FromSeparateConnections_AreSpacedByTheInterval()
    {
        _factory.CreateClient(); // starts the API, which migrates the test database
        var interval = TimeSpan.FromMilliseconds(300);
        var contexts = Enumerable.Range(0, 4).Select(_ => ListingApi.NewDbContext()).ToList();
        try
        {
            // Connections opened beforehand, and each slot compared as a moment (when the call returned +
            // its wait): a wait alone is relative to its own statement, which a slow connection delays.
            foreach (var db in contexts)
            {
                await db.Database.OpenConnectionAsync();
            }

            var slots = await Task.WhenAll(contexts.Select(async db =>
            {
                var wait = await new GeocodingStore(db).ReserveSlotAsync(interval, CancellationToken.None);
                return DateTimeOffset.UtcNow + wait;
            }));

            var sorted = slots.Order().ToList();
            for (var i = 1; i < sorted.Count; i++)
            {
                var gap = sorted[i] - sorted[i - 1];
                Assert.InRange(gap.TotalMilliseconds, 200, 500);
            }
        }
        finally
        {
            foreach (var db in contexts)
            {
                await db.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task AnAnswer_IsStored_AndReplacedByALaterOne()
    {
        _factory.CreateClient();
        var key = $"strada test {Guid.NewGuid():N}";
        await using (var db = ListingApi.NewDbContext())
        {
            var store = new GeocodingStore(db);
            await store.SaveAsync(new GeocodedAddress { Key = key, LookedUpAt = DateTimeOffset.UtcNow }, CancellationToken.None);
            Assert.Null((await store.FindAsync(key, CancellationToken.None))!.Latitude);

            await store.SaveAsync(
                new GeocodedAddress { Key = key, Latitude = 46.99, Longitude = 28.86, FormattedAddress = "Botanica", LookedUpAt = DateTimeOffset.UtcNow },
                CancellationToken.None);
        }

        await using var fresh = ListingApi.NewDbContext();
        var found = await new GeocodingStore(fresh).FindAsync(key, CancellationToken.None);
        Assert.Equal((46.99, 28.86, "Botanica"), (found!.Latitude!.Value, found.Longitude!.Value, found.FormattedAddress));
    }
}
