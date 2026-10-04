using System.Net;
using System.Net.Http.Json;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Favorites.Alerts;
using Imova.Contracts.Account;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Favorites;

// Saved listings through the real endpoints and database: the favorite count only its owner and
// admins see, and one run of the alert job the Worker hosts after a price edit, with its
// unsubscribe link and the account setting behind it.
public class FavoriteAlertsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public FavoriteAlertsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));
    }

    private async Task<(HttpClient Owner, Dictionary<string, object?> Body, ListingDto Listing)> PublishListingAsync(decimal price)
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        body["price"] = price;
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        return (owner, body, listing);
    }

    private async Task<int> RunAlertsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await ActivatorUtilities.CreateInstance<FavoriteAlerts>(scope.ServiceProvider, TimeProvider.System).RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FavoriteCount_IsShownToTheOwnerAndAdmins_NeverToOthers()
    {
        var (owner, _, listing) = await PublishListingAsync(700);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (saver1, _) = await ListingApi.RegisterAsync(_factory);
        var (saver2, _) = await ListingApi.RegisterAsync(_factory);
        var anonymous = _factory.CreateClient();
        (await saver1.PostAsync($"/api/v1/listings/{listing.Id}/favorite", null)).EnsureSuccessStatusCode();
        (await saver2.PostAsync($"/api/v1/listings/{listing.Id}/favorite", null)).EnsureSuccessStatusCode();

        Assert.Equal(2, (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!.FavoriteCount);
        Assert.Equal(2, (await admin.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!.FavoriteCount);
        Assert.Null((await saver1.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!.FavoriteCount);
        Assert.Null((await anonymous.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!.FavoriteCount);

        // "Anunțurile mele" (the owner) and the admin lists have it too; cards for anyone else don't.
        var mine = (await owner.GetFromJsonAsync<List<ListingDto>>("/api/v1/users/me/listings"))!;
        Assert.Equal(2, Assert.Single(mine, l => l.Id == listing.Id).FavoriteCount);
        var moderation = (await admin.GetFromJsonAsync<PagedResult<ListingDto>>($"/api/v1/admin/listings?status=Active&q={listing.Id}"))!;
        Assert.Equal(2, Assert.Single(moderation.Items).FavoriteCount);
        var favorites = (await saver1.GetFromJsonAsync<List<ListingDto>>("/api/v1/users/me/favorites"))!;
        Assert.Null(Assert.Single(favorites, l => l.Id == listing.Id).FavoriteCount);

        // And the raw JSON a visitor gets has no number to read.
        var raw = await anonymous.GetStringAsync($"/api/v1/listings/{listing.Id}");
        Assert.Contains("\"favoriteCount\":null", raw);
    }

    [Fact]
    public async Task PriceEdit_OnAFavoritedListing_EmailsTheOldAndNewPrice_AndTheLinkTurnsTheEmailsOff()
    {
        var (owner, body, listing) = await PublishListingAsync(640);
        var (saver, user) = await ListingApi.RegisterAsync(_factory);
        (await saver.PostAsync($"/api/v1/listings/{listing.Id}/favorite", null)).EnsureSuccessStatusCode();

        body["price"] = 608;
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body)).StatusCode);

        await RunAlertsAsync();

        // (Registration also emailed this address its confirmation link.)
        var email = Assert.Single(_email.SentTo(user.Email), m => m.Subject.StartsWith("Prețul a scăzut"));
        Assert.Contains("Preț vechi: 640 EUR / lună", email.TextBody);
        Assert.Contains("Preț nou: 608 EUR / lună (−5%)", email.TextBody);
        Assert.Contains($"http://localhost:3000/property/{listing.Id}", email.TextBody);
        Assert.Contains("640 EUR / lună", email.HtmlBody);
        Assert.Contains("608 EUR / lună", email.HtmlBody);

        // A second run (same day, nothing new) sends nothing more.
        await RunAlertsAsync();
        Assert.Single(_email.SentTo(user.Email), m => m.Subject.StartsWith("Prețul"));

        var body2 = email.TextBody;
        var link = new Uri(body2[body2.IndexOf("http://localhost:3000/favorites/unsubscribe", StringComparison.Ordinal)..].Split('\n')[0].Trim());
        var query = link.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => Uri.UnescapeDataString(kv[1]));

        var anonymous = _factory.CreateClient();
        var forged = await anonymous.PostAsJsonAsync("/api/v1/favorites/alerts/unsubscribe", new { userId = query["user"], token = "forged" });
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);

        Assert.True((await saver.GetFromJsonAsync<EmailPreferencesDto>("/api/v1/users/me/email-preferences"))!.FavoriteUpdates);
        var unsubscribed = await anonymous.PostAsJsonAsync("/api/v1/favorites/alerts/unsubscribe", new { userId = query["user"], token = query["token"] });
        Assert.Equal(HttpStatusCode.NoContent, unsubscribed.StatusCode);
        Assert.False((await saver.GetFromJsonAsync<EmailPreferencesDto>("/api/v1/users/me/email-preferences"))!.FavoriteUpdates);
    }

    [Fact]
    public async Task MarkedAsRented_EmailsThatItIsNoLongerAvailable()
    {
        var (owner, _, listing) = await PublishListingAsync(520);
        var (saver, user) = await ListingApi.RegisterAsync(_factory);
        (await saver.PostAsync($"/api/v1/listings/{listing.Id}/favorite", null)).EnsureSuccessStatusCode();

        (await owner.PostAsync($"/api/v1/listings/{listing.Id}/mark-as-rented", null)).EnsureSuccessStatusCode();
        await RunAlertsAsync();

        var email = Assert.Single(_email.SentTo(user.Email), m => m.Subject.StartsWith("Nu mai este disponibil"));
        Assert.Contains("Proprietatea a fost închiriată.", email.TextBody);
        Assert.Contains("Ultimul preț: 520 EUR / lună", email.TextBody);
        Assert.Contains($"/property/{listing.Id}#similar-listings-title", email.TextBody);
    }

    [Fact]
    public async Task EmailPreferences_RequireSignIn_AndRoundTrip()
    {
        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/users/me/email-preferences")).StatusCode);

        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var off = await client.PutAsJsonAsync("/api/v1/users/me/email-preferences", new { favoriteUpdates = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await off.Content.ReadFromJsonAsync<EmailPreferencesDto>())!.FavoriteUpdates);

        var on = await client.PutAsJsonAsync("/api/v1/users/me/email-preferences", new { favoriteUpdates = true });
        Assert.True((await on.Content.ReadFromJsonAsync<EmailPreferencesDto>())!.FavoriteUpdates);
    }
}
