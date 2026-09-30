using Imova.Api.Common;
using System.Security.Claims;
using Imova.Application.Features.Listings.ReportListing;
using Imova.Domain.Listings;
using MediatR;

namespace Imova.Api.Features.Listings.ReportListing;

public record ReportListingRequest(ListingReportReason Reason, string? Details);

public static class ReportListingEndpoint
{
    public static void MapReportListing(this IEndpointRouteBuilder app)
    {
        // Signed in only; 404 for a listing that doesn't exist or isn't public, 400 for your own
        // listing, 429 past the daily limit (ListingReports:MaxPerDay).
        app.MapPost("/api/v1/listings/{id:guid}/report", async (
            Guid id,
            ReportListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ReportListingCommand(user.GetUserId(), id, request.Reason, request.Details), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();
    }
}
