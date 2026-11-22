using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.Contracts.Sitemap;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Sitemap;

// The data behind the web app's sitemap.xml: public listings (number + title + last change) and
// public agency pages. Anonymous.
public class SitemapEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    private async Task<IReadOnlyList<SitemapListingDto>> AllListingsAsync(HttpClient client)
    {
        var all = new List<SitemapListingDto>();
        for (var page = 1; ; page++)
        {
            var dto = (await client.GetFromJsonAsync<SitemapListingPageDto>($"/api/v1/sitemap/listings?page={page}"))!;
            all.AddRange(dto.Items);
            if (page * dto.PageSize >= dto.Total) return all;
        }
    }

    [Fact]
    public async Task Listings_HaveTheActiveOnes_NotADraft()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var active = (await (await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner))).Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{active.Id}/approve", null)).EnsureSuccessStatusCode();
        var draft = (await (await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner))).Content.ReadFromJsonAsync<ListingDto>())!;
        var anonymous = _factory.CreateClient();

        var listings = await AllListingsAsync(anonymous);

        var entry = Assert.Single(listings, l => l.Number == active.Number);
        Assert.Equal(active.Title, entry.Title);
        Assert.DoesNotContain(listings, l => l.Number == draft.Number);

        await owner.DeleteAsync($"/api/v1/listings/{active.Id}");
        await owner.DeleteAsync($"/api/v1/listings/{draft.Id}");
    }

    [Fact]
    public async Task Listings_RefuseAPageBeforeTheFirst()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _factory.CreateClient().GetAsync("/api/v1/sitemap/listings?page=0")).StatusCode);
    }

    [Fact]
    public async Task Agencies_AreAnonymous()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/sitemap/agencies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<List<SitemapAgencyDto>>());
    }
}
