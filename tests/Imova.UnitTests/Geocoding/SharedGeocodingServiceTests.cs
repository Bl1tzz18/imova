using Imova.Application.Common.Interfaces;
using Imova.Infrastructure.Geocoding;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Geocoding;

// Every process's geocoding: answers remembered per normalised address, the geocoder called only in a
// reserved slot, failures never remembered, nothing ever thrown.
public class SharedGeocodingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly ManualTimeProvider _clock = new(Now);
    private readonly FakeStore _store = new();
    private readonly FakeClient _client = new();

    private SharedGeocodingService Service() =>
        new(_client, _store, new GeocodingOptions { MinRequestIntervalMilliseconds = 1000 }, _clock, NullLogger<SharedGeocodingService>.Instance);

    [Fact]
    public async Task AnAddress_IsLookedUpOnce_InAReservedSlot_ThenAnsweredFromTheCache()
    {
        _client.Next = new GeocodeLookup(GeocodeOutcome.Found, new GeocodingResult(46.99, 28.86, "bd. Dacia 27, Chișinău"));

        var first = await Service().GeocodeAsync("bd. Dacia 27, Botanica, Chișinău, Moldova", CancellationToken.None);
        var again = await Service().GeocodeAsync("BD DACIA 27 botanica chisinau moldova", CancellationToken.None);

        Assert.Equal((46.99, 28.86), (first!.Latitude, first.Longitude));
        Assert.Equal((46.99, 28.86), (again!.Latitude, again.Longitude));
        Assert.Equal(1, _client.Calls);
        Assert.Equal([TimeSpan.FromSeconds(1)], _store.ReservedIntervals);
        Assert.Equal("bd dacia 27 botanica chisinau moldova", Assert.Single(_store.Rows).Key);
    }

    [Fact]
    public async Task NoSuchPlace_IsRememberedForAWeek_AFailure_NotAtAll()
    {
        _client.Next = GeocodeLookup.NotFound;
        Assert.Null(await Service().GeocodeAsync("Strada Inexistentă 1", CancellationToken.None));
        Assert.Null(await Service().GeocodeAsync("Strada Inexistentă 1", CancellationToken.None));
        Assert.Equal(1, _client.Calls);

        _clock.Now = Now.AddDays(8);
        await Service().GeocodeAsync("Strada Inexistentă 1", CancellationToken.None);
        Assert.Equal(2, _client.Calls);

        _client.Next = GeocodeLookup.Failed;
        await Service().GeocodeAsync("Strada Cu Pană 2", CancellationToken.None);
        await Service().GeocodeAsync("Strada Cu Pană 2", CancellationToken.None);
        Assert.Equal(4, _client.Calls);
        Assert.DoesNotContain(_store.Rows, r => r.Key.Contains("pana"));
    }

    [Fact]
    public async Task AFoundPlace_IsLookedUpAgain_AfterHalfAYear()
    {
        _client.Next = new GeocodeLookup(GeocodeOutcome.Found, new GeocodingResult(47.0, 28.8, "x"));
        await Service().GeocodeAsync("Centru, Chișinău", CancellationToken.None);

        _clock.Now = Now.AddDays(179);
        await Service().GeocodeAsync("Centru, Chișinău", CancellationToken.None);
        Assert.Equal(1, _client.Calls);

        _clock.Now = Now.AddDays(181);
        await Service().GeocodeAsync("Centru, Chișinău", CancellationToken.None);
        Assert.Equal(2, _client.Calls);
    }

    [Fact]
    public async Task ADatabaseProblem_IsNull_NotAnException()
    {
        _store.Broken = true;

        Assert.Null(await Service().GeocodeAsync("Centru, Chișinău", CancellationToken.None));
        Assert.Equal(0, _client.Calls);
    }

    [Theory]
    [InlineData("Str. Dacia, 27, Chișinău", "str dacia 27 chisinau")]
    [InlineData("  STR  DACIA  27   CHIŞINĂU ", "str dacia 27 chisinau")]
    [InlineData("Râșcani — Ţiganu", "rascani tiganu")]
    public void TheCacheKey_IgnoresCaseDiacriticsPunctuationAndSpacing(string address, string key) =>
        Assert.Equal(key, GeocodingKey.Normalize(address));

    [Fact]
    public void AVeryLongAddress_KeepsAUniqueKey_WithinTheColumn()
    {
        var a = GeocodingKey.Normalize(new string('a', 600) + " 1");
        var b = GeocodingKey.Normalize(new string('a', 600) + " 2");

        Assert.Equal(GeocodingKey.MaxLength, a.Length);
        Assert.NotEqual(a, b);
    }

    private sealed class FakeClient : IGeocoderClient
    {
        public GeocodeLookup Next { get; set; } = GeocodeLookup.NotFound;

        public int Calls { get; private set; }

        public Task<GeocodeLookup> LookupAsync(string address, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Next);
        }
    }

    // Answers every slot at once (no wait), recording what was asked.
    private sealed class FakeStore : IGeocodingStore
    {
        public List<GeocodedAddress> Rows { get; } = [];

        public List<TimeSpan> ReservedIntervals { get; } = [];

        public bool Broken { get; set; }

        public Task<GeocodedAddress?> FindAsync(string key, CancellationToken cancellationToken) =>
            Broken ? throw new InvalidOperationException("database down") : Task.FromResult(Rows.FirstOrDefault(r => r.Key == key));

        public Task SaveAsync(GeocodedAddress address, CancellationToken cancellationToken)
        {
            Rows.RemoveAll(r => r.Key == address.Key);
            Rows.Add(address);
            return Task.CompletedTask;
        }

        public Task<TimeSpan> ReserveSlotAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            ReservedIntervals.Add(interval);
            return Task.FromResult(TimeSpan.Zero);
        }
    }
}
