using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Agencies;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Agencies;
using Imova.Infrastructure;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Imova.IntegrationTests.Agencies;

// The public side of agencies: the page by slug (and its redirect after a rename), the directory,
// the phone reveal, the search filter for an agency's listings, and the count on a listing's page.
public class AgencyPublicEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgencyPublicEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private static object Profile(string name) => new { name, phone = "+373 22 555 010", email = "office@agentie.md" };

    private static async Task<AgencyDto> CreateAgencyAsync(HttpClient owner, string? name = null)
    {
        var response = await owner.PostAsJsonAsync("/api/v1/agencies", Profile(name ?? $"Agenția Publică {Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AgencyDto>())!;
    }

    // An Active listing under the agency: published by its owner, approved by an admin.
    private async Task<ListingDto> PublishActiveAsync(HttpClient owner, Guid agencyId)
    {
        var body = await ListingApi.ValidBodyAsync(owner);
        body["agencyId"] = agencyId;
        var created = await owner.PostAsJsonAsync("/api/v1/listings", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var listing = (await created.Content.ReadFromJsonAsync<ListingDto>())!;

        var admin = await ListingApi.RegisterAdminAsync(_factory);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).StatusCode);
        return listing;
    }

    // Straight in the database — the deactivate endpoint has its own tests (AgencyLifecycleEndpointsTests).
    private async Task DeactivateAsync(Guid agencyId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ImovaDbContext>();
        var agency = await db.Agencies.SingleAsync(a => a.Id == agencyId);
        db.Entry(agency).Property(a => a.Status).CurrentValue = AgencyStatus.Deactivated;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task BySlug_IsPublic_AndCountsOnlyActiveListings()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        await PublishActiveAsync(owner, agency.Id);

        var response = await _factory.CreateClient().GetAsync($"/api/v1/agencies/by-slug/{agency.Slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<AgencyDto>())!;
        Assert.Equal(agency.Id, dto.Id);
        Assert.Equal(1, dto.ActiveListingCount);
        Assert.Null(dto.Phone);
        Assert.Null(dto.MyRole);
    }

    [Fact]
    public async Task BySlug_AfterARename_TheOldSlugAnswers301WithTheNewOne()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var renamed = await owner.PutAsJsonAsync($"/api/v1/agencies/{agency.Id}", Profile($"Agenția Redenumită {Guid.NewGuid():N}"));
        var newSlug = (await renamed.Content.ReadFromJsonAsync<AgencyDto>())!.Slug;
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/api/v1/agencies/by-slug/{agency.Slug}");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal($"/api/v1/agencies/by-slug/{newSlug}", response.Headers.Location?.OriginalString);
        Assert.Equal(newSlug, (await response.Content.ReadFromJsonAsync<AgencySlugRedirectDto>())!.Slug);
    }

    [Fact]
    public async Task BySlug_UnknownOrDeactivated_Is404_ButAMemberStillSeesIt()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        await DeactivateAsync(agency.Id);
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/agencies/by-slug/nu-exista-{Guid.NewGuid():N}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/agencies/by-slug/{agency.Slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsync($"/api/v1/agencies/{agency.Id}/contact/phone", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/agencies/by-slug/{agency.Slug}")).StatusCode);
    }

    [Fact]
    public async Task Directory_FindsTheAgencyByNameWithoutDiacritics_AndLeavesOutDeactivatedOnes()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var marker = Guid.NewGuid().ToString("N")[..10];
        var active = await CreateAgencyAsync(owner, $"Agenția Șoim {marker}");
        var deactivated = await CreateAgencyAsync(owner, $"Agenția Șoim Închisă {marker}");
        await DeactivateAsync(deactivated.Id);
        await PublishActiveAsync(owner, active.Id);

        var result = (await _factory.CreateClient().GetFromJsonAsync<PagedResult<AgencyCardDto>>($"/api/v1/agencies?q=soim {marker}"))!;

        var card = Assert.Single(result.Items);
        Assert.Equal(active.Id, card.Id);
        Assert.Equal(1, card.ActiveListingCount);
    }

    [Fact]
    public async Task Directory_PageSizeOverTheMaximum_Is400()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/agencies?pageSize=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PhoneReveal_GivesThePublicTheFullNumber()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var anonymous = _factory.CreateClient();

        var page = (await anonymous.GetFromJsonAsync<AgencyDto>($"/api/v1/agencies/by-slug/{agency.Slug}"))!;
        var response = await anonymous.PostAsync($"/api/v1/agencies/{agency.Id}/contact/phone", null);

        Assert.Null(page.Phone);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(agency.Phone, (await response.Content.ReadFromJsonAsync<AgencyPhoneDto>())!.Phone);
    }

    [Fact]
    public async Task Search_ByAgency_ReturnsOnlyThatAgencysActiveListings()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var other = await CreateAgencyAsync(owner);
        var listing = await PublishActiveAsync(owner, agency.Id);
        await PublishActiveAsync(owner, other.Id);

        var result = (await _factory.CreateClient().GetFromJsonAsync<PagedResult<ListingDto>>($"/api/v1/listings/search?agencyId={agency.Id}"))!;

        Assert.Equal(listing.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task ListingDetail_CarriesTheAgencysActiveListingCount_ButSearchCardsDont()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var listing = await PublishActiveAsync(owner, agency.Id);
        await PublishActiveAsync(owner, agency.Id);
        var anonymous = _factory.CreateClient();

        var detail = (await anonymous.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        var cards = (await anonymous.GetFromJsonAsync<PagedResult<ListingDto>>($"/api/v1/listings/search?agencyId={agency.Id}"))!;

        Assert.Equal(2, detail.Agency!.ActiveListingCount);
        Assert.All(cards.Items, card => Assert.Null(card.Agency!.ActiveListingCount));
    }
}
