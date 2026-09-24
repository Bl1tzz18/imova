using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// Requires the compose Postgres (and Azurite) to be reachable — Program.cs runs EF migrations on
// startup. Ownership now goes through the listing's Publisher (Publisher.UserId), not a user id
// stored on the listing itself.
public class ListingOwnershipTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ListingOwnershipTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private async Task<(HttpClient OwnerClient, HttpClient OtherClient, ListingDto Listing, Dictionary<string, object?> Body)> CreateAsOwnerAsync()
    {
        var (ownerClient, _) = await ListingApi.RegisterAsync(_factory);
        var (otherClient, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(ownerClient);

        var response = await ownerClient.PostAsJsonAsync("/api/v1/listings", body);
        response.EnsureSuccessStatusCode();
        return (ownerClient, otherClient, (await response.Content.ReadFromJsonAsync<ListingDto>())!, body);
    }

    [Fact]
    public async Task CreateListing_PublishesUnderTheCallersIndividualPublisher_IgnoringSpoofedIds()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["ownerId"] = Guid.NewGuid();
        body["requestingUserId"] = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal(user.Id, listing.Publisher.UserId);
        Assert.Equal("Individual", listing.Publisher.PublisherType);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task CreateListing_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateListing_ByOwner_Succeeds()
    {
        var (ownerClient, _, listing, body) = await CreateAsOwnerAsync();
        body["title"] = "Updated title";
        body["price"] = 600;

        var response = await ownerClient.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal(600m, updated.Price.Amount);

        await ownerClient.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task UpdateListing_ByNonOwner_Returns403AndLeavesListingUnchanged()
    {
        var (ownerClient, otherClient, listing, body) = await CreateAsOwnerAsync();
        body["title"] = "Hijacked title";

        var response = await otherClient.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var unchanged = (await ownerClient.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Equal(listing.Title, unchanged.Title);

        await ownerClient.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task DeleteListing_ByNonOwner_Returns403AndLeavesListingIntact()
    {
        var (ownerClient, otherClient, listing, _) = await CreateAsOwnerAsync();

        var response = await otherClient.DeleteAsync($"/api/v1/listings/{listing.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);

        await ownerClient.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task DeleteListing_ByOwner_Succeeds()
    {
        var (ownerClient, _, listing, _) = await CreateAsOwnerAsync();

        var response = await ownerClient.DeleteAsync($"/api/v1/listings/{listing.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);
    }

    [Fact]
    public async Task PendingListing_IsHiddenFromOtherUsersButVisibleToItsOwner()
    {
        var (ownerClient, otherClient, listing, _) = await CreateAsOwnerAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);
        var mine = (await ownerClient.GetFromJsonAsync<List<ListingDto>>("/api/v1/users/me/listings"))!;
        Assert.Contains(mine, l => l.Id == listing.Id);

        await ownerClient.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }
}
