using System.Net;
using Imova.Infrastructure.Geocoding;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Geocoding;

// The Nominatim client on its own: a place, "no such place", or a failure — told apart so only real
// answers get cached (SharedGeocodingService). Throttling and caching are SharedGeocodingService's.
public class NominatimGeocodingServiceTests
{
    private static NominatimGeocodingService CreateService(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") }, NullLogger<NominatimGeocodingService>.Instance);

    [Fact]
    public async Task AMatch_IsFound_WithParsedCoordinates()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"lat":"47.0105","lon":"28.8638","display_name":"Chisinau, Moldova"}]"""),
        });

        var lookup = await CreateService(handler).LookupAsync("Chisinau, Moldova", CancellationToken.None);

        Assert.Equal(GeocodeOutcome.Found, lookup.Outcome);
        Assert.Equal((47.0105, 28.8638, "Chisinau, Moldova"), (lookup.Result!.Latitude, lookup.Result.Longitude, lookup.Result.FormattedAddress));
    }

    [Fact]
    public async Task Requests_AreRestrictedToMoldova()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });

        await CreateService(handler).LookupAsync("Botanica, Chisinau, Moldova", CancellationToken.None);

        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("countrycodes=md", uri);
        Assert.Contains("format=json", uri);
    }

    [Fact]
    public async Task AnEmptyResultsArray_IsNotFound()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });

        var lookup = await CreateService(handler).LookupAsync("Nonexistent Place, Moldova", CancellationToken.None);

        Assert.Equal(GeocodeOutcome.NotFound, lookup.Outcome);
        Assert.Null(lookup.Result);
    }

    public static TheoryData<string> Failures() => new() { "429", "network", "json", "coordinate" };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task ErrorsAndBadAnswers_AreFailures_NeverThrown(string failure)
    {
        var handler = new FakeHttpMessageHandler(_ => failure switch
        {
            "429" => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            "network" => throw new HttpRequestException("network unreachable"),
            "json" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not valid json") },
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""[{"lat":"x","lon":"28.8","display_name":"?"}]""") },
        });

        var lookup = await CreateService(handler).LookupAsync("Chisinau, Moldova", CancellationToken.None);

        Assert.Equal(GeocodeOutcome.Failed, lookup.Outcome);
    }

    [Fact]
    public async Task ABlankAddress_IsNotLookedUp()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("should not be called"));

        var lookup = await CreateService(handler).LookupAsync("   ", CancellationToken.None);

        Assert.Equal(GeocodeOutcome.NotFound, lookup.Outcome);
        Assert.Null(handler.LastRequest);
    }
}
