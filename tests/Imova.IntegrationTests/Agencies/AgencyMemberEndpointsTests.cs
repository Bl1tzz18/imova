using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Agencies;

// Inviting someone into an agency, all the way through the emailed link, and the member rules.
public class AgencyMemberEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public AgencyMemberEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));
    }

    private static async Task<AgencyDto> CreateAgencyAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/api/v1/agencies", new
        {
            name = $"Agenția Echipa {Guid.NewGuid():N}",
            phone = "+373 22 555 010",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AgencyDto>())!;
    }

    private string TokenSentTo(string address) =>
        Regex.Match(_email.SentTo(address).Last().TextBody, @"/invitations/([A-Za-z0-9_-]+)").Groups[1].Value;

    [Fact]
    public async Task AnInvitation_ByEmail_IsAcceptedByTheInvitedAccountOnly()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (invitee, inviteeUser) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);

        var invite = await owner.PostAsJsonAsync($"/api/v1/agencies/{agency.Id}/invitations", new { email = inviteeUser.Email, role = "Agent" });
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);
        var token = TokenSentTo(inviteeUser.Email);

        var shown = (await _factory.CreateClient().GetFromJsonAsync<InvitationDto>($"/api/v1/invitations/{token}"))!;
        Assert.Equal(agency.Name, shown.AgencyName);
        Assert.Equal("Pending", shown.Status);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/v1/invitations/{token}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await invitee.PostAsync($"/api/v1/invitations/{token}/accept", null)).StatusCode);

        var members = (await owner.GetFromJsonAsync<List<AgencyMemberDto>>($"/api/v1/agencies/{agency.Id}/members"))!;
        Assert.Contains(members, m => m.UserId == inviteeUser.Id && m.Role == "Agent");

        var again = await owner.PostAsJsonAsync($"/api/v1/agencies/{agency.Id}/invitations", new { email = inviteeUser.Email, role = "Agent" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Contains("agency.alreadyMember", await again.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/agencies/{agency.Id}/members/{inviteeUser.Id}")).StatusCode);
        members = (await owner.GetFromJsonAsync<List<AgencyMemberDto>>($"/api/v1/agencies/{agency.Id}/members"))!;
        Assert.DoesNotContain(members, m => m.UserId == inviteeUser.Id);
    }

    [Fact]
    public async Task TheMemberList_IsNotForOutsiders()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/agencies/{agency.Id}/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/v1/agencies/{agency.Id}/members")).StatusCode);
    }

    [Fact]
    public async Task TheLastOwner_CantLeave()
    {
        var (owner, ownerUser) = await ListingApi.RegisterAsync(_factory);
        var agency = await CreateAgencyAsync(owner);

        var response = await owner.DeleteAsync($"/api/v1/agencies/{agency.Id}/members/{ownerUser.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("agency.lastOwner", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnUnknownInvitationLink_Is404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync("/api/v1/invitations/not-a-real-token")).StatusCode);
    }
}
