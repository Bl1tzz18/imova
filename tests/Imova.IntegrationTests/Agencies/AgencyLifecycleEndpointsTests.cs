using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.Agencies;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Agencies;

// Step 7 over HTTP: deactivate / reactivate (and what the public sees meanwhile), delete, and the
// admins' verification.
public class AgencyLifecycleEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgencyLifecycleEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private static async Task<AgencyDto> CreateAgencyAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync(
            "/api/v1/agencies", new { name = $"Agenția Ciclu {Guid.NewGuid():N}", phone = "+373 22 555 010" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AgencyDto>())!;
    }

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

    private async Task<int> SearchCountAsync(Guid agencyId)
    {
        var json = await _factory.CreateClient().GetStringAsync($"/api/v1/listings/search?agencyId={agencyId}");
        return JsonDocument.Parse(json).RootElement.GetProperty("totalCount").GetInt32();
    }

    private static HttpRequestMessage Delete(Guid agencyId, string? confirmName) =>
        new(HttpMethod.Delete, $"/api/v1/agencies/{agencyId}") { Content = JsonContent.Create(new { confirmName }) };

    [Fact]
    public async Task Deactivating_HidesTheAgencyAndItsListings_UntilReactivated()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var listing = await PublishActiveAsync(owner, agency.Id);
        var anonymous = _factory.CreateClient();
        Assert.Equal(1, await SearchCountAsync(agency.Id));

        var deactivated = await owner.PostAsync($"/api/v1/agencies/{agency.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.Equal("Deactivated", (await deactivated.Content.ReadFromJsonAsync<AgencyDto>())!.Status);
        Assert.Equal(0, await SearchCountAsync(agency.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/agencies/by-slug/{agency.Slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsync($"/api/v1/listings/{listing.Id}/contact/phone", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/v1/agencies/{agency.Id}/reactivate", null)).StatusCode);
        Assert.Equal(1, await SearchCountAsync(agency.Id));
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);
    }

    [Fact]
    public async Task OutsidersCantDeactivate()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/v1/agencies/{agency.Id}/deactivate", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsync($"/api/v1/agencies/{agency.Id}/deactivate", null)).StatusCode);
    }

    [Fact]
    public async Task Deleting_WithTheExactName_KeepsTheListingsAsPrivate()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var listing = await PublishActiveAsync(owner, agency.Id);

        var wrong = await owner.SendAsync(Delete(agency.Id, "nu e numele"));
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Contains("agency.confirmNameMismatch", await wrong.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await owner.SendAsync(Delete(agency.Id, agency.Name))).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/agencies/{agency.Id}")).StatusCode);
        var after = (await _factory.CreateClient().GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Null(after.Agency);
        Assert.Equal("Active", after.Status);
        Assert.DoesNotContain(
            (await owner.GetFromJsonAsync<List<MyAgencyDto>>("/api/v1/users/me/agencies"))!, a => a.Id == agency.Id);
    }

    [Fact]
    public async Task OnlySiteAdmins_Verify_AndSeeTheAdminList()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var admin = await ListingApi.RegisterAdminAsync(_factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsync($"/api/v1/admin/agencies/{agency.Id}/verify", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/v1/admin/agencies")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/v1/admin/agencies/{agency.Id}/verify", null)).StatusCode);
        Assert.True((await owner.GetFromJsonAsync<AgencyDto>($"/api/v1/agencies/{agency.Id}"))!.IsVerified);

        var list = (await admin.GetFromJsonAsync<PagedResult<AdminAgencyDto>>(
            $"/api/v1/admin/agencies?q={Uri.EscapeDataString(agency.Name)}"))!;
        var row = Assert.Single(list.Items);
        Assert.True(row.IsVerified);
        Assert.Equal(agency.Id, row.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/v1/admin/agencies/{agency.Id}/unverify", null)).StatusCode);
        Assert.False((await owner.GetFromJsonAsync<AgencyDto>($"/api/v1/agencies/{agency.Id}"))!.IsVerified);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync($"/api/v1/admin/agencies/{Guid.NewGuid()}/verify", null)).StatusCode);
    }
}
