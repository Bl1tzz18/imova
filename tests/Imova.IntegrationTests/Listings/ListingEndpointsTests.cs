using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.Amenities;
using Imova.Contracts.Proximities;
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
        Assert.True(listing.RentalDetails!.PetsAllowed);
        Assert.Equal(12, listing.RentalDetails.MinLeasePeriodMonths);

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
        body["typeSpecificAttributes"] = new Dictionary<string, object?> { ["plotType"] = "Forest", ["rooms"] = 2 };

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
        body["typeSpecificAttributes"] = ListingApi.CompleteAttributes("Land");

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal("Land", listing.Property.PropertyType);
        Assert.Equal("Agricultural", listing.Property.TypeSpecificAttributes.GetProperty("plotType").GetString());
        Assert.NotNull(listing.SaleDetails);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Theory]
    [InlineData("Apartment", "Rent")]
    [InlineData("House", "Sale")]
    [InlineData("Land", "Sale")]
    [InlineData("Commercial", "Rent")]
    [InlineData("Garage", "Sale")]
    [InlineData("Room", "Rent")]
    public async Task CreateListing_CompleteListingOfEachType_RoundTripsEveryDetail(string propertyType, string transactionType)
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        // Every amenity the form would offer for this type (Furnished included, for rentals too) —
        // none for Land.
        var amenities = (await client.GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!
            .Where(a => a.ApplicablePropertyTypes.Contains(propertyType))
            .ToList();
        Assert.Equal(propertyType == "Land", amenities.Count == 0);
        // Every proximity applies to every type.
        var proximities = (await client.GetFromJsonAsync<List<ProximityDto>>("/api/v1/proximities"))!;
        var attributes = ListingApi.CompleteAttributes(propertyType);
        var body = await ListingApi.ValidBodyAsync(client);
        body["propertyType"] = propertyType;
        body["transactionType"] = transactionType;
        body["totalAreaM2"] = propertyType == "Land" ? 12_000 : 85;
        body["yearBuilt"] = propertyType == "Land" ? null : 2012;
        // Garage and Room keep the general condition; the others use finishCondition.
        body["condition"] = propertyType is "Garage" or "Room" ? "Renovated" : null;
        // Pets are asked only for a rented home (apartment, house, room).
        var petsApply = propertyType is "Apartment" or "House" or "Room";
        body["rentalDetails"] = transactionType == "Rent"
            ? new Dictionary<string, object?> { ["petsAllowed"] = petsApply ? false : null, ["utilitiesIncluded"] = true }
            : null;
        body["amenityIds"] = amenities.Select(a => a.Id).ToArray();
        body["proximityIds"] = proximities.Select(p => p.Id).ToArray();
        body["typeSpecificAttributes"] = attributes;

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        var stored = (await client.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{created.Id}"))!;
        Assert.Equal(propertyType, stored.Property.PropertyType);
        foreach (var (key, expected) in attributes)
        {
            var actual = stored.Property.TypeSpecificAttributes.GetProperty(key);
            Assert.Equal(
                Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture)!.ToLowerInvariant(),
                actual.ValueKind == JsonValueKind.String ? actual.GetString()!.ToLowerInvariant() : actual.GetRawText().ToLowerInvariant());
        }

        Assert.Equal(amenities.Select(a => a.Key).Order(), stored.Property.Amenities.Select(a => a.Key).Order());
        Assert.Equal(proximities.Select(p => p.Key), stored.Property.Proximities.Select(p => p.Key));
        Assert.Equal(propertyType is "Garage" or "Room" ? "Renovated" : null, stored.Property.Condition);

        await client.DeleteAsync($"/api/v1/listings/{created.Id}");
    }

    [Fact]
    public async Task CreateListing_WithAnAmenityThatDoesNotApplyToTheType_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var sauna = (await client.GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!.Single(a => a.Key == "sauna");
        var body = await ListingApi.ValidBodyAsync(client);
        body["propertyType"] = "Garage";
        body["typeSpecificAttributes"] = ListingApi.CompleteAttributes("Garage");
        body["amenityIds"] = new[] { sauna.Id };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_LandWithASoilScoreForANonAgriculturalPlot_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        var attributes = ListingApi.CompleteAttributes("Land");
        attributes["plotType"] = "ForConstruction";
        body["propertyType"] = "Land";
        body["yearBuilt"] = null;
        body["typeSpecificAttributes"] = attributes;

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("TypeSpecificAttributes.SoilQualityScore", out _));
    }

    [Fact]
    public async Task CreateListing_HouseWithDistrictHeatingButABoilerEnergySource_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["propertyType"] = "House";
        body["condition"] = null;
        body["typeSpecificAttributes"] = new Dictionary<string, object?>
        {
            ["rooms"] = 3, ["houseType"] = "Individual", ["buildingMaterial"] = "Brick", ["finishCondition"] = "NoRepair",
            ["houseFloors"] = 1, ["livingAreaM2"] = 90, ["landAreaM2"] = 400, ["heatingSystem"] = "DistrictHeating",
            ["heatingEnergySource"] = "Gas", ["waterSupply"] = "CentralNetwork", ["sewerage"] = "Central",
            ["gasSupply"] = false, ["floorMaterial"] = "Laminate", ["roofMaterial"] = "Metal", ["windowType"] = "Thermopane",
        };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("TypeSpecificAttributes.HeatingEnergySource", out _));
    }

    [Fact]
    public async Task CreateListing_RentalWithTheFurnishedAmenity_IsAllowed()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var furnished = (await client.GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!.Single(a => a.Key == "furnished");
        var body = await ListingApi.ValidBodyAsync(client);
        body["amenityIds"] = new[] { furnished.Id };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Contains(listing.Property.Amenities, a => a.Key == "furnished");
        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task CreateListing_RentedApartmentWithoutThePetsAnswer_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["rentalDetails"] = new Dictionary<string, object?> { ["minLeasePeriodMonths"] = 12 };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("RentalDetails.PetsAllowed", out _));
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
        Assert.Null(inBand.Single(l => l.Id == listing.Id).Contact);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task HiddenPhone_IsNotInThePublicResponse_ButTheOwnerStillSeesIt()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        body["contact"] = new Dictionary<string, object?>
        {
            ["personType"] = "Self",
            ["phone"] = "+373 68 987 654",
            ["messagingApps"] = new[] { "WhatsApp" },
            ["preferredContactMethod"] = "PlatformMessages",
            ["hidePhoneNumber"] = true,
        };
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).StatusCode);

        var publicJson = await _factory.CreateClient().GetStringAsync($"/api/v1/listings/{listing.Id}");
        var ownerView = (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;

        // Neither the listing's contact number nor the account's own number leaks anywhere.
        Assert.DoesNotContain("987 654", publicJson);
        Assert.DoesNotContain("123 456", publicJson);
        var publicView = JsonSerializer.Deserialize<ListingDto>(publicJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Null(publicView.Contact!.Phone);
        Assert.True(publicView.Contact.HidePhoneNumber);
        Assert.Equal("PlatformMessages", publicView.Contact.PreferredContactMethod);
        Assert.Empty(publicView.Contact.MessagingApps);
        Assert.Equal("+373 68 987 654", ownerView.Contact!.Phone);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task HiddenPhone_WithCallsPreferred_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["contact"] = new Dictionary<string, object?>
        {
            ["personType"] = "Self", ["phone"] = "+373 68 987 654", ["preferredContactMethod"] = "PhoneCall", ["hidePhoneNumber"] = true,
        };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("Contact.PreferredContactMethod", out _));
    }

    [Fact]
    public async Task OtherContact_RoundTripsItsOwnDetails()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body["contact"] = new Dictionary<string, object?>
        {
            ["personType"] = "Other",
            ["name"] = "Maria Popescu",
            ["phone"] = "+373 79 333 444",
            ["email"] = "maria@example.com",
            ["preferredContactMethod"] = "PhoneCall",
            ["callHoursFrom"] = "09:00",
            ["callHoursTo"] = "18:00",
        };

        var created = (await (await client.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        var contact = (await client.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{created.Id}"))!.Contact!;

        Assert.Equal("Other", contact.PersonType);
        Assert.Equal("Maria Popescu", contact.Name);
        Assert.Equal("maria@example.com", contact.Email);
        Assert.Equal("+373 79 333 444", contact.Phone);
        Assert.Equal("09:00", contact.CallHoursFrom);
        Assert.Equal("18:00", contact.CallHoursTo);

        await client.DeleteAsync($"/api/v1/listings/{created.Id}");
    }

    [Fact]
    public async Task CreateListing_WithoutAContact_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(client);
        body.Remove("contact");

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task ApprovedListing_ExpiresInSixMonths_AndTheOwnerCanRenewIt()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var listing = (await (await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client)))
            .Content.ReadFromJsonAsync<ListingDto>())!;

        // Not live yet: nothing to renew.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/v1/listings/{listing.Id}/renew", null)).StatusCode);

        var approved = (await (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null))
            .Content.ReadFromJsonAsync<ListingDto>())!;
        var sixMonths = DateTimeOffset.UtcNow.AddMonths(6);
        Assert.InRange(approved.ExpiresAt!.Value, sixMonths.AddMinutes(-5), sixMonths.AddMinutes(5));

        var stranger = (await ListingApi.RegisterAsync(_factory)).Client;
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/v1/listings/{listing.Id}/renew", null)).StatusCode);

        var renew = await client.PostAsync($"/api/v1/listings/{listing.Id}/renew", null);
        Assert.Equal(HttpStatusCode.OK, renew.StatusCode);
        var renewed = (await renew.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal("Active", renewed.Status);
        Assert.True(renewed.ExpiresAt >= approved.ExpiresAt);

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
        Assert.Equal(user.Id, publisher.UserId);
        Assert.Equal("Listing Test User", publisher.DisplayName);
        Assert.Equal("+373 69 123 456", publisher.Phone);
    }


    [Fact]
    public async Task CreateListing_IsAlwaysTheCallersOwn_WhateverPublisherTheBodyNames()
    {
        var (victim, victimUser) = await ListingApi.RegisterAsync(_factory);
        var victimPublisher = (await victim.GetFromJsonAsync<List<PublisherDto>>("/api/v1/publishers/mine"))!.Single();
        var (attacker, attackerUser) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(attacker);
        // No longer part of the API (the author is the caller; an agency is chosen with agencyId) — ignored.
        body["publisherId"] = victimPublisher.Id;

        var response = await attacker.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal(attackerUser.Id, listing.Publisher.UserId);
        Assert.NotEqual(victimUser.Id, listing.Publisher.UserId);
    }

    [Fact]
    public async Task GetAmenities_ReturnsTheSeededList()
    {
        var amenities = (await _factory.CreateClient().GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!;

        Assert.True(amenities.Count >= 10);
        Assert.Contains(amenities, a => a.Key == "parking" && a.LabelRo == "Parcare");
        Assert.DoesNotContain(amenities, a => a.Key is "near_water" or "near_forest" or "guarded");
    }

    [Fact]
    public async Task GetProximities_ReturnsTheSeededListForEveryType()
    {
        var proximities = (await _factory.CreateClient().GetFromJsonAsync<List<ProximityDto>>("/api/v1/proximities"))!;

        Assert.Equal(10, proximities.Count);
        Assert.Equal("kindergarten", proximities[0].Key);
        Assert.Contains(proximities, p => p.Key == "public_transport" && p.LabelRo == "Stație transport public");
        Assert.All(proximities, p => Assert.Equal(6, p.ApplicablePropertyTypes.Count));
    }

    [Fact]
    public async Task CreateListing_WithAnAmenityIdAsAProximity_Returns400()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var balcony = (await client.GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!.Single(a => a.Key == "balcony");
        var body = await ListingApi.ValidBodyAsync(client);
        body["proximityIds"] = new[] { balcony.Id };

        var response = await client.PostAsJsonAsync("/api/v1/listings", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("ProximityIds", out _));
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
