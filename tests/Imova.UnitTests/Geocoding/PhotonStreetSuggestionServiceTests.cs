using System.Net;
using Imova.Infrastructure.Geocoding;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Geocoding;

public class PhotonStreetSuggestionServiceTests
{
    private static PhotonStreetSuggestionService CreateService(FakeHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://photon.test/") },
            // No throttle delay in tests — the real pacing is exercised for real in production,
            // not something worth slowing the suite for (same reasoning as NominatimGeocodingServiceTests).
            new PhotonOptions { MinRequestIntervalMilliseconds = 0 },
            NullLogger<PhotonStreetSuggestionService>.Instance);

    private const string TwoFeatureResponse = """
        {
          "features": [
            { "geometry": { "coordinates": [28.8638, 47.0105] }, "properties": { "name": "Strada Ismail" } },
            { "geometry": { "coordinates": [28.87, 47.02] }, "properties": { "name": "Strada Alexandru cel Bun" } }
          ]
        }
        """;

    [Fact]
    public async Task SuggestStreetsAsync_WithMatchingFeatures_ReturnsNamesAndCoordinates()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TwoFeatureResponse),
        });
        var service = CreateService(handler);

        var result = await service.SuggestStreetsAsync("Ismail", null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Strada Ismail", result[0].Name);
        Assert.Equal(47.0105, result[0].Latitude);
        Assert.Equal(28.8638, result[0].Longitude);
    }

    [Fact]
    public async Task SuggestStreetsAsync_RestrictsToMoldovaBoundingBoxAndStreetTag()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"features":[]}"""),
        });
        var service = CreateService(handler);

        await service.SuggestStreetsAsync("Ismail", null, CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("osm_tag=highway", uri);
        Assert.Contains("bbox=26.6172,45.4494,30.1596,48.4918", uri);
    }

    [Fact]
    public async Task SuggestStreetsAsync_WithLocality_FoldsItIntoTheQueryText()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"features":[]}"""),
        });
        var service = CreateService(handler);

        await service.SuggestStreetsAsync("Ismail", "Chișinău", CancellationToken.None);

        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("Ismail Chișinău", uri);
    }

    [Fact]
    public async Task SuggestStreetsAsync_DeduplicatesRepeatedStreetNames()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "features": [
                    { "geometry": { "coordinates": [28.86, 47.01] }, "properties": { "name": "Strada Ismail" } },
                    { "geometry": { "coordinates": [28.87, 47.02] }, "properties": { "name": "Strada Ismail" } }
                  ]
                }
                """),
        });
        var service = CreateService(handler);

        var result = await service.SuggestStreetsAsync("Ismail", null, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task SuggestStreetsAsync_WithNonSuccessStatusCode_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var service = CreateService(handler);

        var result = await service.SuggestStreetsAsync("Ismail", null, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestStreetsAsync_WhenHttpClientThrows_ReturnsEmptyListInsteadOfPropagating()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network unreachable"));
        var service = CreateService(handler);

        var result = await service.SuggestStreetsAsync("Ismail", null, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestStreetsAsync_WithMalformedJson_ReturnsEmptyListInsteadOfPropagating()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json"),
        });
        var service = CreateService(handler);

        var result = await service.SuggestStreetsAsync("Ismail", null, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestStreetsAsync_WithBlankQuery_ReturnsEmptyListWithoutMakingARequest()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("should not be called"));
        var service = CreateService(handler);

        var result = await service.SuggestStreetsAsync("   ", null, CancellationToken.None);

        Assert.Empty(result);
        Assert.Null(handler.LastRequest);
    }
}
