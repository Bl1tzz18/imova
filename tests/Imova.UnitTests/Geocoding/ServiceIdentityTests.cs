using System.Net;
using Imova.Infrastructure.Geocoding;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Geocoding;

// What Nominatim and Photon learn about us: an identifying User-Agent with a way to reach us, and
// Nominatim's `email` parameter — both from configuration (ServiceIdentity), as their policies ask.
public class ServiceIdentityTests
{
    [Fact]
    public void UserAgent_NamesTheAppItsSiteAndTheContactAddress()
    {
        var identity = new ServiceIdentityOptions { AppName = "IMOVA", AppVersion = "2.1", Website = "https://imova.md", ContactEmail = "osm@imova.md" };

        Assert.Equal("IMOVA/2.1 (+https://imova.md; osm@imova.md)", identity.UserAgent);
    }

    [Fact]
    public void UserAgent_WithoutAContactAddress_LeavesItOut()
    {
        Assert.Equal("IMOVA/1.0 (+https://imova.md)", new ServiceIdentityOptions().UserAgent);
    }

    [Fact]
    public void TheUserAgent_IsAValidHeaderValue()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(new ServiceIdentityOptions { ContactEmail = "osm@imova.md" }.UserAgent);

        Assert.Equal("IMOVA/1.0 (+https://imova.md; osm@imova.md)", client.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public async Task Nominatim_GetsTheContactAddressAsItsEmailParameter()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        var service = new NominatimGeocodingService(
            new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") },
            new ServiceIdentityOptions { ContactEmail = "osm+geo@imova.md" },
            NullLogger<NominatimGeocodingService>.Instance);

        await service.LookupAsync("Strada Ismail 1, Chișinău", CancellationToken.None);

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("&email=osm%2Bgeo%40imova.md", uri);
        Assert.Contains("countrycodes=md", uri);
    }

    [Fact]
    public void Nominatim_WithoutAContactAddress_SendsNoEmailParameter()
    {
        var service = new NominatimGeocodingService(new HttpClient(), new ServiceIdentityOptions(), NullLogger<NominatimGeocodingService>.Instance);

        Assert.DoesNotContain("email=", service.RequestUri("Chișinău"));
    }
}
