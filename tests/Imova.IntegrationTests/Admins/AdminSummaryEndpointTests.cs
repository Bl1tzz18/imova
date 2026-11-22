using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Admins;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Admins;

// GET /api/v1/admin/summary — the "things waiting" counts behind the account menu's
// "Administrare" entry. The test database is shared with other tests, so only the shape and the
// access rules are asserted here; the counting itself is unit-tested.
public class AdminSummaryEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    [Fact]
    public async Task AnAdmin_GetsTheCounts()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);

        var summary = await admin.GetFromJsonAsync<AdminSummaryDto>("/api/v1/admin/summary");

        Assert.NotNull(summary);
        Assert.True(summary!.PendingReviewListings >= 0 && summary.ReportedListings >= 0
            && summary.OpenConversationReports >= 0 && summary.FlaggedMessages >= 0);
    }

    [Fact]
    public async Task NonAdmins_AndAnonymousCallers_AreRefused()
    {
        var (user, _) = await ListingApi.RegisterAsync(_factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/v1/admin/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/v1/admin/summary")).StatusCode);
    }
}
