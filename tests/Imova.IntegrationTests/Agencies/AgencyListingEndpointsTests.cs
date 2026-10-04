using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Agencies;
using Imova.Contracts.Listings;
using Imova.Contracts.Messaging;
using Imova.Domain.Agencies;
using Imova.Infrastructure;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Imova.IntegrationTests.Agencies;

// Publishing listings under an agency, and who may then manage them.
public class AgencyListingEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgencyListingEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private static async Task<AgencyDto> CreateAgencyAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/api/v1/agencies", new { name = $"Agenția Anunțuri {Guid.NewGuid():N}", phone = "+373 22 555 010" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AgencyDto>())!;
    }

    // Membership straight in the database — inviting has its own tests (AgencyMemberEndpointsTests).
    private async Task AddMemberAsync(Guid agencyId, Guid userId, AgencyRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ImovaDbContext>();
        var agency = await db.Agencies.Include(a => a.Members).SingleAsync(a => a.Id == agencyId);
        agency.AddMember(userId, role, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid? agencyId)
    {
        var body = await ListingApi.ValidBodyAsync(client);
        body["agencyId"] = agencyId;
        return await client.PostAsJsonAsync("/api/v1/listings", body);
    }

    [Fact]
    public async Task AMember_PublishesUnderTheirAgency_AndSeesItAmongTheirAgencies()
    {
        var (owner, ownerUser) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);

        var response = await PublishAsync(owner, agency.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal(agency.Id, listing.Agency!.Id);
        Assert.Equal(agency.Slug, listing.Agency.Slug);
        Assert.Equal(ownerUser.Id, listing.Publisher.UserId);

        var mine = (await owner.GetFromJsonAsync<List<MyAgencyDto>>("/api/v1/users/me/agencies"))!;
        var entry = Assert.Single(mine, a => a.Id == agency.Id);
        Assert.Equal("Owner", entry.Role);
        Assert.Equal("Active", entry.Status);
    }

    [Fact]
    public async Task SomeoneOutsideTheAgency_CantPublishUnderIt()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);

        var response = await PublishAsync(stranger, agency.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("listing.notAgencyMember", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnEditThatLeavesOutAgencyId_IsRefused()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = (await (await PublishAsync(owner, null)).Content.ReadFromJsonAsync<ListingDto>())!;
        var body = await ListingApi.ValidBodyAsync(owner);
        body.Remove("agencyId");

        var response = await owner.PutAsJsonAsync($"/api/v1/listings/{created.Id}", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheAgencysOwner_EditsAnAgentsListing_ButAnotherAgentCant()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (agent, agentUser) = await ListingApi.RegisterAsync(_factory);
        var (otherAgent, otherAgentUser) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        await AddMemberAsync(agency.Id, agentUser.Id, AgencyRole.Agent);
        await AddMemberAsync(agency.Id, otherAgentUser.Id, AgencyRole.Agent);
        var listing = (await (await PublishAsync(agent, agency.Id)).Content.ReadFromJsonAsync<ListingDto>())!;

        var body = await ListingApi.ValidBodyAsync(owner);
        body["agencyId"] = agency.Id;
        body["title"] = "Apartament renovat, editat de agenție";

        var byOwner = await owner.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body);
        var byOtherAgent = await otherAgent.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body);

        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        Assert.Equal("Apartament renovat, editat de agenție", (await byOwner.Content.ReadFromJsonAsync<ListingDto>())!.Title);
        Assert.Equal(HttpStatusCode.Forbidden, byOtherAgent.StatusCode);
    }

    [Fact]
    public async Task AVisitorWritingAboutAnAgencyListing_SeesTheAgentAndTheAgency()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (visitor, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);
        var listing = (await (await PublishAsync(owner, agency.Id)).Content.ReadFromJsonAsync<ListingDto>())!;
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        Assert.True((await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).IsSuccessStatusCode);

        var start = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = listing.Id, body = "Bună ziua!" });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var inbox = (await visitor.GetFromJsonAsync<List<ConversationSummaryDto>>("/api/v1/messaging/conversations"))!;
        var row = Assert.Single(inbox, c => c.Listing.Id == listing.Id);
        Assert.Equal(agency.Name, row.OtherParticipant.AgencyName);
    }
}
