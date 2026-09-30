using Imova.Api.Common;
using System.Security.Claims;
using Imova.Application.Common.Identity;
using Imova.Application.Features.ListingReports.DismissListingReports;
using Imova.Application.Features.ListingReports.GetListingReportSummary;
using Imova.Application.Features.ListingReports.GetReportedListings;
using MediatR;

namespace Imova.Api.Features.ListingReports;

public record DismissListingReportsRequest(string? Note);

// The admin side of listing reports. Acting on a reported listing = suspending it through the
// usual POST /api/v1/listings/{id}/suspend (which closes its reports), or dismissing them here.
public static class ListingReportEndpoints
{
    public static void MapListingReportEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin/listing-reports").RequireAdmin();

        // ?resolved=false (open cases, the default) | true (the decisions history); ?page=, ?pageSize=
        admin.MapGet("", async (ClaimsPrincipal user, ISender sender, CancellationToken ct, bool resolved = false, int page = 1, int pageSize = 20) =>
            Results.Ok(await sender.Send(new GetReportedListingsQuery(user.IsInRole(Roles.Admin), resolved, page, pageSize), ct)));

        admin.MapGet("/summary", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetListingReportSummaryQuery(user.IsInRole(Roles.Admin)), ct)));

        admin.MapPost("/{listingId:guid}/dismiss", async (
            Guid listingId, DismissListingReportsRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DismissListingReportsCommand(user.IsInRole(Roles.Admin), user.GetUserId(), listingId, request.Note), ct);
            return Results.NoContent();
        });
    }
}
