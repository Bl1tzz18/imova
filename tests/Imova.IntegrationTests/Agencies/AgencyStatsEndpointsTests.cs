using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Agencies;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Agencies;

// The statistics endpoint end to end: a view and a phone reveal counted by the real SQL show up in
// today's totals; only members (and admins) may look.
public class AgencyStatsEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    [Fact]
    public async Task CountedViewsAndReveals_ShowUpInTheAgencysStatistics_ForMembersOnly()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var agency = (await (await owner.PostAsJsonAsync("/api/v1/agencies", new { name = $"Agenția Cifre {Guid.NewGuid():N}", phone = "+373 22 555 010" }))
            .Content.ReadFromJsonAsync<AgencyDto>())!;
        var body = await ListingApi.ValidBodyAsync(owner);
        body["agencyId"] = agency.Id;
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();

        var anonymous = _factory.CreateClient();
        foreach (var path in new[] { "views", "contact/phone" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/listings/{listing.Id}/{path}");
            request.Headers.Add("X-Imova-Visitor", Guid.NewGuid().ToString());
            (await anonymous.SendAsync(request)).EnsureSuccessStatusCode();
        }

        var stats = (await owner.GetFromJsonAsync<AgencyStatsDto>($"/api/v1/agencies/{agency.Id}/stats"))!;
        Assert.Equal(1, stats.Totals.Views);
        Assert.Equal(1, stats.Totals.ListingPhoneReveals);
        Assert.Equal(1, stats.Totals.Contacts);
        Assert.Equal(30, stats.Daily.Count);
        Assert.Equal(listing.Id, Assert.Single(stats.TopListings).ListingId);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/agencies/{agency.Id}/stats")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/agencies/{agency.Id}/stats")).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await owner.GetAsync($"/api/v1/agencies/{agency.Id}/stats?from=2026-10-09&to=2026-10-01")).StatusCode);
    }
}
