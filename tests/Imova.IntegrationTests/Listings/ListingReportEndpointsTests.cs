using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.Auth;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// Reporting a listing, and the admin queue that acts on the reports. The queue is shared by every
// test in the run, so assertions look for their own listing in it.
public class ListingReportEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    private async Task<(HttpClient Owner, ListingDto Listing)> ActiveListingAsync(HttpClient admin)
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner));
        var listing = (await created.Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        return (owner, listing);
    }

    private static Task<HttpResponseMessage> ReportAsync(HttpClient client, Guid listingId, string reason = "Fraud", string? details = null) =>
        client.PostAsJsonAsync($"/api/v1/listings/{listingId}/report", new { reason, details });

    private static async Task<ReportedListingDto?> CaseAsync(HttpClient admin, Guid listingId, bool resolved = false) =>
        (await admin.GetFromJsonAsync<PagedResult<ReportedListingDto>>($"/api/v1/admin/listing-reports?resolved={resolved}&pageSize=50"))!
            .Items.FirstOrDefault(c => c.Listing.Id == listingId);

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("errorCodes", out var codes)
            ? codes[0].GetProperty("code").GetString()
            : body.RootElement.GetProperty("code").GetString();
    }

    [Fact]
    public async Task AVisitorsReport_ReachesTheAdminQueue_WithWhoReportedItAndWhy()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (_, listing) = await ActiveListingAsync(admin);
        var (visitor, visitorUser) = await ListingApi.RegisterAsync(_factory);

        var first = await ReportAsync(visitor, listing.Id, "WrongInformation");
        var again = await ReportAsync(visitor, listing.Id, "Fraud", "Cere avans de 200 € înainte de vizionare.");

        Assert.False((await first.Content.ReadFromJsonAsync<ReportListingResultDto>())!.Amended);
        Assert.True((await again.Content.ReadFromJsonAsync<ReportListingResultDto>())!.Amended);

        var reported = (await CaseAsync(admin, listing.Id))!;
        var report = Assert.Single(reported.Reports);
        Assert.Equal("Fraud", report.Reason);
        Assert.Equal("Cere avans de 200 € înainte de vizionare.", report.Details);
        Assert.Equal(visitorUser.Id, report.Reporter.UserId);
        Assert.Equal(visitorUser.Email, report.Reporter.Email);
        Assert.Equal(1, report.Reporter.ReportsFiled);
        Assert.Equal("Active", reported.Listing.Status);

        var summary = await admin.GetFromJsonAsync<ListingReportSummaryDto>("/api/v1/admin/listing-reports/summary");
        Assert.True(summary!.OpenListings >= 1);
        Assert.True(summary.OpenReports >= 1);
    }

    [Fact]
    public async Task SuspendingAReportedListing_ClosesItsReports()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (_, listing) = await ActiveListingAsync(admin);
        var (visitor, _) = await ListingApi.RegisterAsync(_factory);
        (await ReportAsync(visitor, listing.Id)).EnsureSuccessStatusCode();

        (await admin.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/suspend", new { reason = "Fraudă: cere bani în avans." }))
            .EnsureSuccessStatusCode();

        Assert.Null(await CaseAsync(admin, listing.Id));
        var decided = (await CaseAsync(admin, listing.Id, resolved: true))!;
        Assert.Equal("ListingSuspended", decided.Resolution!.Outcome);
        Assert.Equal("Fraudă: cere bani în avans.", decided.Resolution.Note);
        Assert.Equal("Suspended", decided.Listing.Status);

        // Suspended = no longer public, so it can't be reported any more.
        Assert.Equal(HttpStatusCode.NotFound, (await ReportAsync(visitor, listing.Id)).StatusCode);
    }

    [Fact]
    public async Task DismissingTheReports_KeepsTheListing_AndASecondDismissSaysTheyWereHandled()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (_, listing) = await ActiveListingAsync(admin);
        var (visitor, _) = await ListingApi.RegisterAsync(_factory);
        (await ReportAsync(visitor, listing.Id, "NoLongerAvailable")).EnsureSuccessStatusCode();

        var dismissed = await admin.PostAsJsonAsync($"/api/v1/admin/listing-reports/{listing.Id}/dismiss", new { note = "Încă disponibil." });
        var twice = await admin.PostAsJsonAsync($"/api/v1/admin/listing-reports/{listing.Id}/dismiss", new { note = (string?)null });

        Assert.Equal(HttpStatusCode.NoContent, dismissed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
        Assert.Equal("listingReport.noneOpen", await ErrorCodeAsync(twice));
        var decided = (await CaseAsync(admin, listing.Id, resolved: true))!;
        Assert.Equal("Dismissed", decided.Resolution!.Outcome);
        Assert.Equal("Active", decided.Listing.Status);
    }

    [Fact]
    public async Task Reporting_NeedsASignedInVisitor_AndAValidReport()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (owner, listing) = await ActiveListingAsync(admin);
        var (visitor, _) = await ListingApi.RegisterAsync(_factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ReportAsync(_factory.CreateClient(), listing.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReportAsync(visitor, Guid.NewGuid())).StatusCode);

        var own = await ReportAsync(owner, listing.Id);
        Assert.Equal(HttpStatusCode.BadRequest, own.StatusCode);
        Assert.Equal("listingReport.ownListing", await ErrorCodeAsync(own));

        var noDetails = await ReportAsync(visitor, listing.Id, "Other");
        Assert.Equal(HttpStatusCode.BadRequest, noDetails.StatusCode);
        Assert.Equal("report.detailsRequired", await ErrorCodeAsync(noDetails));

        Assert.Equal(HttpStatusCode.BadRequest, (await ReportAsync(visitor, listing.Id, "NotAReason")).StatusCode);
    }

    [Fact]
    public async Task TheDailyLimit_Is429WithACode()
    {
        var limited = _factory.WithWebHostBuilder(b => b.UseSetting("ListingReports:MaxPerDay", "1"));
        using var admin = await ListingApi.RegisterAdminAsync(limited);
        var (_, first) = await ActiveListingAsync(admin);
        var (_, second) = await ActiveListingAsync(admin);
        var (visitor, _) = await ListingApi.RegisterAsync(limited);

        (await ReportAsync(visitor, first.Id)).EnsureSuccessStatusCode();
        var blocked = await ReportAsync(visitor, second.Id);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("listingReport.limitReached", await ErrorCodeAsync(blocked));
    }
}
