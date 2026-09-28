using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.SavedSearches.Alerts;
using Imova.Contracts.Listings;
using Imova.Contracts.SavedSearches;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.SavedSearches;

// Saved searches through the real endpoints and the real (Postgres) search, plus one alert run of
// the job the Worker hosts. The test database is shared across runs, so every test searches for a
// price no other listing has.
public class SavedSearchEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public SavedSearchEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));
    }

    private static decimal UniquePrice() => 100_000 + Random.Shared.Next(1, 800_000) + 0.37m;

    private static string SearchFor(decimal price)
    {
        var p = price.ToString(CultureInfo.InvariantCulture);
        return $"transactionType=Rent&minPriceEur={p}&maxPriceEur={p}&page=2";
    }

    private static async Task<SavedSearchDto> SaveAsync(HttpClient client, string queryString, string alertFrequency = "Instant")
    {
        var response = await client.PostAsJsonAsync("/api/v1/saved-searches", new { name = "Chirii test", queryString, alertFrequency });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SavedSearchDto>())!;
    }

    private async Task<ListingDto> PublishListingAsync(decimal price)
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        body["price"] = price;
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        return listing;
    }

    private static async Task<SavedSearchDto> OnlySavedSearchAsync(HttpClient client) =>
        Assert.Single((await client.GetFromJsonAsync<List<SavedSearchDto>>("/api/v1/saved-searches"))!);

    [Fact]
    public async Task SavedSearches_RequireSignIn()
    {
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/saved-searches")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/v1/saved-searches", new { name = "x", queryString = "", alertFrequency = "Off" })).StatusCode);
    }

    [Fact]
    public async Task Save_NormalizesTheSearch_AndSavingItAgainUpdatesInsteadOfDuplicating()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);

        var first = await SaveAsync(client, "propertyType=House&page=3&transactionType=Sale");
        var again = await SaveAsync(client, "transactionType=Sale&propertyType=House", "Daily");

        Assert.Equal("propertyType=House&transactionType=Sale", first.QueryString);
        Assert.Equal(first.Id, again.Id);
        Assert.Equal("Daily", (await OnlySavedSearchAsync(client)).AlertFrequency);
    }

    [Fact]
    public async Task Save_AnInvalidSearch_Returns400WithACode()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/saved-searches", new { name = "x", queryString = "minPriceEur=900&maxPriceEur=100", alertFrequency = "Off" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errorCodes", out _));
    }

    [Fact]
    public async Task AnotherUsersSavedSearch_IsNotFound()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var saved = await SaveAsync(owner, "transactionType=Sale");

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await stranger.PutAsJsonAsync($"/api/v1/saved-searches/{saved.Id}", new { name = "mine now", alertFrequency = "Off" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/api/v1/saved-searches/{saved.Id}/viewed", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/v1/saved-searches/{saved.Id}")).StatusCode);
        Assert.Empty((await stranger.GetFromJsonAsync<List<SavedSearchDto>>("/api/v1/saved-searches"))!);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/saved-searches/{saved.Id}")).StatusCode);
    }

    [Fact]
    public async Task NewListingsCount_CountsWhatWasPublishedSinceTheLastVisit()
    {
        var price = UniquePrice();
        var (watcher, _) = await ListingApi.RegisterAsync(_factory);
        var saved = await SaveAsync(watcher, SearchFor(price));
        Assert.Equal(0, (await OnlySavedSearchAsync(watcher)).NewListingsCount);

        await PublishListingAsync(price);
        Assert.Equal(1, (await OnlySavedSearchAsync(watcher)).NewListingsCount);

        var viewed = await watcher.PostAsync($"/api/v1/saved-searches/{saved.Id}/viewed", null);
        Assert.Equal(HttpStatusCode.OK, viewed.StatusCode);
        Assert.Equal(0, (await OnlySavedSearchAsync(watcher)).NewListingsCount);
    }

    [Fact]
    public async Task AnAlertRun_EmailsTheNewListing_AndItsUnsubscribeLinkTurnsAlertsOff()
    {
        var price = UniquePrice();
        var (watcher, user) = await ListingApi.RegisterAsync(_factory);
        var saved = await SaveAsync(watcher, SearchFor(price), "Instant");
        var listing = await PublishListingAsync(price);

        // The job leaves listings published in the last SettleDelay to the next run — run it "later".
        using (var scope = _factory.Services.CreateScope())
        {
            var later = new OffsetTimeProvider(SavedSearchAlerts.SettleDelay + TimeSpan.FromSeconds(5));
            var alerts = ActivatorUtilities.CreateInstance<SavedSearchAlerts>(scope.ServiceProvider, (TimeProvider)later);
            await alerts.RunAsync(CancellationToken.None);
        }

        // (Registration also emailed this address its confirmation link.)
        var email = Assert.Single(_email.SentTo(user.Email), m => m.Subject.Contains("„Chirii test”"));
        Assert.Contains($"/property/{listing.Id}", email.TextBody);

        var body = email.TextBody;
        var unsubscribeLink = new Uri(body[body.IndexOf("http://localhost:3000/saved-searches/unsubscribe", StringComparison.Ordinal)..].Trim());
        var query = unsubscribeLink.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => Uri.UnescapeDataString(kv[1]));

        var anonymous = _factory.CreateClient();
        var forged = await anonymous.PostAsJsonAsync("/api/v1/saved-searches/unsubscribe", new { id = saved.Id, token = "forged" });
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);

        var unsubscribed = await anonymous.PostAsJsonAsync("/api/v1/saved-searches/unsubscribe", new { id = query["id"], token = query["token"] });
        Assert.Equal(HttpStatusCode.OK, unsubscribed.StatusCode);
        Assert.Equal("Off", (await OnlySavedSearchAsync(watcher)).AlertFrequency);
    }

    private sealed class OffsetTimeProvider(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + offset;
    }
}
