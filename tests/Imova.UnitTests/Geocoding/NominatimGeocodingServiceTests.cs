using System.Net;
using Imova.Infrastructure.Geocoding;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Geocoding;

public class NominatimGeocodingServiceTests
{
    private static NominatimGeocodingService CreateService(FakeHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") },
            // No throttle delay in tests — the real 1 req/sec pacing is exercised for real by
            // NominatimGeocodingService in production, not something worth slowing the suite for.
            new GeocodingOptions { MinRequestIntervalMilliseconds = 0 },
            NullLogger<NominatimGeocodingService>.Instance);

    [Fact]
    public async Task GeocodeAsync_WithAMatchingResult_ReturnsParsedCoordinates()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"lat":"47.0105","lon":"28.8638","display_name":"Chisinau, Moldova"}]"""),
        });
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("Chisinau, Moldova", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(47.0105, result!.Latitude);
        Assert.Equal(28.8638, result.Longitude);
        Assert.Equal("Chisinau, Moldova", result.FormattedAddress);
    }

    [Fact]
    public async Task GeocodeAsync_RequestsRestrictToMoldova()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        });
        var service = CreateService(handler);

        await service.GeocodeAsync("Botanica, Chisinau, Moldova", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("countrycodes=md", uri);
        Assert.Contains("format=json", uri);
    }

    [Fact]
    public async Task GeocodeAsync_WithEmptyResultsArray_ReturnsNull()
    {
        // An empty array is Nominatim's "nothing matched this address" response.
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        });
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("Nonexistent Place, Moldova", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_WithNonSuccessStatusCode_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("Chisinau, Moldova", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_WhenHttpClientThrows_ReturnsNullInsteadOfPropagating()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network unreachable"));
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("Chisinau, Moldova", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_WithMalformedJson_ReturnsNullInsteadOfPropagating()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json"),
        });
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("Chisinau, Moldova", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_WithBlankAddress_ReturnsNullWithoutMakingARequest()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("should not be called"));
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("   ", CancellationToken.None);

        Assert.Null(result);
        Assert.Null(handler.LastRequest);
    }
}
