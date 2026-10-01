using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// The public phone number flow and the view / phone-reveal counters against real Postgres (the
// counting is one raw SQL statement).
public class ListingVisitorTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string VisitorHeader = "X-Imova-Visitor";
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    private async Task<(HttpClient Owner, ListingDto Listing)> ActiveListingAsync(WebApplicationFactory<Program>? app = null)
    {
        app ??= _factory;
        var (owner, _) = await ListingApi.RegisterAsync(app);
        var admin = await ListingApi.RegisterAdminAsync(app);
        var body = await ListingApi.ValidBodyAsync(owner);
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        return (owner, listing);
    }

    private static async Task<HttpResponseMessage> PostAsVisitor(HttpClient client, string url, string visitor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(VisitorHeader, visitor);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task ThePublicGetsTheNumbersShape_AndTheNumberOnlyByAskingForIt()
    {
        var (owner, listing) = await ActiveListingAsync();
        var anonymous = _factory.CreateClient();

        var publicContact = (await anonymous.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!.Contact!;
        Assert.Null(publicContact.Phone);
        Assert.Equal("+373691", publicContact.PhonePrefix);
        Assert.Equal(5, publicContact.PhoneHiddenDigits);

        var revealed = await PostAsVisitor(anonymous, $"/api/v1/listings/{listing.Id}/contact/phone", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, revealed.StatusCode);
        Assert.Equal("+373 69 111 222", (await revealed.Content.ReadFromJsonAsync<ListingPhoneDto>())!.Phone);

        // The owner still sees their own number in the listing.
        Assert.Equal("+373 69 111 222", (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!.Contact!.Phone);

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsync($"/api/v1/listings/{Guid.NewGuid()}/contact/phone", null)).StatusCode);
        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task Views_CountOncePerVisitor_NeverTheOwner_AndBothCountsAreTheOwnersToSee()
    {
        var (owner, listing) = await ActiveListingAsync();
        var anonymous = _factory.CreateClient();
        var (visitorA, visitorB) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.NoContent, (await PostAsVisitor(anonymous, $"/api/v1/listings/{listing.Id}/views", visitorA)).StatusCode);
        await PostAsVisitor(anonymous, $"/api/v1/listings/{listing.Id}/views", visitorA);
        await PostAsVisitor(anonymous, $"/api/v1/listings/{listing.Id}/views", visitorB);
        await PostAsVisitor(owner, $"/api/v1/listings/{listing.Id}/views", Guid.NewGuid().ToString());
        await PostAsVisitor(anonymous, $"/api/v1/listings/{listing.Id}/contact/phone", visitorA);
        await PostAsVisitor(anonymous, $"/api/v1/listings/{listing.Id}/contact/phone", visitorA);

        var publicView = (await anonymous.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        var ownerView = (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Null(publicView.ViewCount);
        Assert.Null(publicView.PhoneRevealCount);
        Assert.Equal(2, ownerView.ViewCount);
        Assert.Equal(1, ownerView.PhoneRevealCount);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task EveryListing_GetsItsOwnShortNumber()
    {
        var (owner, first) = await ActiveListingAsync();
        var second = (await (await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner))).Content.ReadFromJsonAsync<ListingDto>())!;

        Assert.True(first.Number >= 100_000);
        Assert.True(second.Number > first.Number);

        await owner.DeleteAsync($"/api/v1/listings/{first.Id}");
        await owner.DeleteAsync($"/api/v1/listings/{second.Id}");
    }

    [Fact]
    public async Task AskingForTooManyNumbers_IsRateLimited()
    {
        var limited = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("RateLimiting:PhoneReveal:Enabled", "true");
            b.UseSetting("RateLimiting:PhoneReveal:PermitLimit", "2");
        });
        var (owner, listing) = await ActiveListingAsync(limited);
        var anonymous = limited.CreateClient();
        var url = $"/api/v1/listings/{listing.Id}/contact/phone";

        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsync(url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsync(url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await anonymous.PostAsync(url, null)).StatusCode);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }
}
