using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// The admin moderation lists (?status=) behind the moderation page's tabs, with suspend/reinstate.
public class AdminListingModerationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    [Fact]
    public async Task SuspendedListing_MovesFromTheActiveListToTheSuspendedOne_AndBackOnReinstate()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var created = await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner));
        var listing = (await created.Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();

        Assert.Contains(await IdsAsync(admin, "Active"), id => id == listing.Id);
        var byLink = await admin.GetFromJsonAsync<PagedResult<ListingDto>>(
            $"/api/v1/admin/listings?status=Active&q={Uri.EscapeDataString($"http://localhost:3000/property/{listing.Id}")}");
        Assert.Equal(listing.Id, Assert.Single(byLink!.Items).Id);

        (await admin.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/suspend", new { reason = "Fotografii înșelătoare." }))
            .EnsureSuccessStatusCode();
        var suspended = await admin.GetFromJsonAsync<PagedResult<ListingDto>>("/api/v1/admin/listings?status=Suspended&pageSize=100");
        var row = Assert.Single(suspended!.Items, l => l.Id == listing.Id);
        Assert.Equal("Fotografii înșelătoare.", row.SuspensionReason);
        Assert.DoesNotContain(await IdsAsync(admin, "Active"), id => id == listing.Id);

        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/reinstate", null)).EnsureSuccessStatusCode();
        Assert.DoesNotContain(await IdsAsync(admin, "Suspended"), id => id == listing.Id);
    }

    [Fact]
    public async Task ModerationRows_CarryTheListingsViewsAndPhoneReveals()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner))).Content.ReadFromJsonAsync<ListingDto>())!;

        var pending = await admin.GetFromJsonAsync<PagedResult<ListingDto>>("/api/v1/admin/listings?status=PendingReview&pageSize=100");
        var row = Assert.Single(pending!.Items, l => l.Id == listing.Id);
        Assert.Equal(0, row.ViewCount);
        Assert.Equal(0, row.PhoneRevealCount);
        Assert.True(row.Number >= 100_000);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task TheLists_AreAdminOnly_AndOnlyForModerationStatuses()
    {
        var (user, _) = await ListingApi.RegisterAsync(_factory);
        using var admin = await ListingApi.RegisterAdminAsync(_factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/v1/admin/listings?status=Active")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/admin/listings?status=Draft")).StatusCode);
    }

    // Every moderation route: refused before anything runs — the same answer whether the listing
    // exists or not and whatever the body, so nothing leaks to a non-admin.
    public static TheoryData<string, string> AdminRoutes() => new()
    {
        { "GET", "/api/v1/admin/listings?status=Active" },
        { "GET", "/api/v1/admin/listings/pending-review" },
        { "POST", "/api/v1/listings/{0}/approve" },
        { "POST", "/api/v1/listings/{0}/reject" },
        { "POST", "/api/v1/listings/{0}/suspend" },
        { "POST", "/api/v1/listings/{0}/reinstate" },
        { "GET", "/api/v1/admin/listing-reports" },
        { "GET", "/api/v1/admin/listing-reports/summary" },
        { "POST", "/api/v1/admin/listing-reports/{0}/dismiss" },
    };

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task AdminRoutes_RefuseAnonymousCallersAndNonAdmins_ForExistingAndUnknownListings(string method, string route)
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner));
        var listing = (await created.Content.ReadFromJsonAsync<ListingDto>())!;
        var anonymous = _factory.CreateClient();

        foreach (var id in new[] { listing.Id, Guid.NewGuid() })
        {
            var url = string.Format(route, id);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(anonymous, method, url, new { reason = "x" })).StatusCode);
            // The owner is signed in but not an admin — with a valid body and with an empty one.
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(owner, method, url, new { reason = "Motiv" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(owner, method, url, new { })).StatusCode);
        }

        var after = (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Equal(listing.Status, after.Status);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url, object body) =>
        method == "GET" ? client.GetAsync(url) : client.PostAsJsonAsync(url, body);

    private static async Task<List<Guid>> IdsAsync(HttpClient admin, string status) =>
        (await admin.GetFromJsonAsync<PagedResult<ListingDto>>($"/api/v1/admin/listings?status={status}&pageSize=100"))!
            .Items.Select(l => l.Id).ToList();
}
