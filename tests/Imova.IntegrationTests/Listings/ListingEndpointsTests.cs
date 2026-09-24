using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.Amenities;
using Imova.Contracts.Listings;
using Imova.Contracts.Publishers;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

public class ListingEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ListingEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    [Fact]
    public async Task CreateListing_ReturnsPropertyWithTypedAttributesAmenitiesAndRentalTerms()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var amenities = (await client.GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!;
        var body = await ListingApi.ValidBodyAsync(client);
        body["amenityIds"] = amenities.Where(a => a.Key is "balcony" or "elevator").Select(a => a.Id).ToArray();

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal("PendingReview", listing.Status);
        Assert.Equal(9, listing.Property.TypeSpecificAttributes.GetProperty("totalFloors").GetInt32());
        Assert.Equal(["balcony", "elevator"], listing.Property.Amenities.Select(a => a.Key).Order());
        Assert.Equal("Furnished", listing.RentalDetails!.FurnishedStatus);
        Assert.True(listing.RentalDetails.PetsAllowed);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task CreateListing_LandWithApartmentOnlyFields_Returns400NamingTheField()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["propertyType"] = "Land";
        body["yearBuilt"] = null;
        body["condition"] = null;
        body["typeSpecificAttributes"] = new Dictionary<string, object?> { ["landDesignation"] = "Intravilan", ["rooms"] = 2 };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            "'rooms' is not a field of PropertyType Land.",
            problem.GetProperty("errors").GetProperty("TypeSpecificAttributes").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task CreateListing_LandWithItsOwnFields_Succeeds()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["propertyType"] = "Land";
        body["totalAreaM2"] = 1200;
        body["yearBuilt"] = null;
        body["condition"] = null;
        body["transactionType"] = "Sale";
        body["rentalDetails"] = null;
        body["typeSpecificAttributes"] = new Dictionary<string, object?>
        {
            ["landDesignation"] = "Construction",
            ["roadAccess"] = "Paved",
            ["utilitiesAtBoundary"] = new Dictionary<string, object?> { ["electricity"] = true, ["water"] = true },
        };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal("Land", listing.Property.PropertyType);
        Assert.Equal("Construction", listing.Property.TypeSpecificAttributes.GetProperty("landDesignation").GetString());
        Assert.NotNull(listing.SaleDetails);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task ApprovedListing_AppearsInPublicSearch_FilteredByPriceEurAcrossCurrencies()
    {
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        // A unique price band so other data in the dev database can't interfere.
        var marker = 900_000 + Random.Shared.Next(1, 90_000);
        body["price"] = marker * 20; // in MDL, ~= marker * 20 * 0.051 EUR with the default rate
        body["currency"] = "MDL";
        var listing = (await (await client.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;

        var publicBefore = (await client.GetFromJsonAsync<List<ListingDto>>("/api/v1/listings"))!;
        Assert.DoesNotContain(publicBefore, l => l.Id == listing.Id);

        var approve = await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var priceEur = listing.Price.PriceEur;
        var inBand = (await client.GetFromJsonAsync<List<ListingDto>>(
            $"/api/v1/listings?transactionType=Rent&minPriceEur={priceEur - 1}&maxPriceEur={priceEur + 1}"))!;
        var outOfBand = (await client.GetFromJsonAsync<List<ListingDto>>(
            $"/api/v1/listings?minPriceEur={priceEur + 1}&maxPriceEur={priceEur + 2}"))!;
        Assert.Contains(inBand, l => l.Id == listing.Id);
        Assert.DoesNotContain(outOfBand, l => l.Id == listing.Id);
        // Contact details stay off search results.
        Assert.Null(inBand.Single(l => l.Id == listing.Id).Publisher.Phone);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task ApproveListing_ByNonAdmin_Returns403()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var listing = (await (await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client)))
            .Content.ReadFromJsonAsync<ListingDto>())!;

        var response = await client.PostAsync($"/api/v1/listings/{listing.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task ArchivingAPendingListing_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var listing = (await (await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client)))
            .Content.ReadFromJsonAsync<ListingDto>())!;

        var response = await client.PostAsync($"/api/v1/listings/{listing.Id}/archive", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task GetListings_WithUnknownTransactionType_Returns400()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/listings?transactionType=Lease");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_GivesTheUserAnIndividualPublisher()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);

        var publishers = (await client.GetFromJsonAsync<List<PublisherDto>>("/api/v1/publishers/mine"))!;

        var publisher = Assert.Single(publishers);
        Assert.Equal("Individual", publisher.PublisherType);
        Assert.Equal(user.Id, publisher.UserId);
        Assert.Equal("Listing Test User", publisher.DisplayName);
        Assert.Equal("+373 69 123 456", publisher.Phone);
    }

    [Fact]
    public async Task AgencyPublisher_CanBeCreatedOnceAndUsedToPublish()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);

        var create = await client.PostAsJsonAsync("/api/v1/publishers/agency", new
        {
            displayName = "Imobil Grup",
            phone = "+373 22 000 000",
            bio = "Agenție imobiliară.",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var agency = (await create.Content.ReadFromJsonAsync<PublisherDto>())!;
        Assert.Equal("Agency", agency.PublisherType);
        Assert.Equal(user.Email, agency.Email);

        var second = await client.PostAsJsonAsync("/api/v1/publishers/agency", new { displayName = "Alta", phone = "+373 22 000 001" });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        var mine = (await client.GetFromJsonAsync<List<PublisherDto>>("/api/v1/publishers/mine"))!;
        Assert.Equal(["Individual", "Agency"], mine.Select(p => p.PublisherType));

        var body = await ListingApi.ValidBodyAsync(client);
        body["publisherId"] = agency.Id;
        var listing = (await (await client.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal(agency.Id, listing.Publisher.Id);
        Assert.Equal("Imobil Grup", listing.Publisher.DisplayName);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task CreateListing_UnderAnotherUsersPublisher_Returns403()
    {
        var (victim, _) = await ListingApi.RegisterAsync(_factory);
        var victimPublisher = (await victim.GetFromJsonAsync<List<PublisherDto>>("/api/v1/publishers/mine"))!.Single();
        var (attacker, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(attacker);
        body["publisherId"] = victimPublisher.Id;

        var response = await attacker.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAmenities_ReturnsTheSeededList()
    {
        var amenities = (await _factory.CreateClient().GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!;

        Assert.True(amenities.Count >= 10);
        Assert.Contains(amenities, a => a.Key == "parking" && a.LabelRo == "Parcare");
    }

    [Fact]
    public async Task Favorites_CanBeSavedForAListingAndListed()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var listing = (await (await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client)))
            .Content.ReadFromJsonAsync<ListingDto>())!;

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/listings/{listing.Id}/favorite", null)).StatusCode);
        var favorites = (await client.GetFromJsonAsync<List<ListingDto>>("/api/v1/users/me/favorites"))!;
        Assert.True(Assert.Single(favorites).IsSaved);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }
}
