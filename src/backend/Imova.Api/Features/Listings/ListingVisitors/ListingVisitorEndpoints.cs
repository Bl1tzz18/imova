using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.RecordListingView;
using Imova.Application.Features.Listings.RevealListingPhone;
using MediatR;

namespace Imova.Api.Features.Listings.ListingVisitors;

// Anonymous visitors are told apart by the web app's visitor cookie, forwarded as this header (a
// signed-in visitor by their account).
public static class ListingVisitorEndpoints
{
    public const string VisitorHeader = "X-Imova-Visitor";

    public static void MapListingVisitorEndpoints(this IEndpointRouteBuilder app)
    {
        // Always 204: whether it counted (or the listing exists) is nobody's business.
        app.MapPost("/api/v1/listings/{id:guid}/views", async (
            Guid id,
            HttpRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(new RecordListingViewCommand(id, UserId(user), request.Headers[VisitorHeader].FirstOrDefault()), cancellationToken);
            return Results.NoContent();
        }).RequireRateLimiting(ListingVisitorRateLimiting.ViewPolicy);

        // The full phone number, one request at a time (rate-limited per IP). 404 unless the listing is
        // Active and has a number its owner didn't hide.
        app.MapPost("/api/v1/listings/{id:guid}/contact/phone", async (
            Guid id,
            HttpRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var phone = await sender.Send(
                new RevealListingPhoneCommand(id, UserId(user), user.IsInRole(Roles.Admin), request.Headers[VisitorHeader].FirstOrDefault()),
                cancellationToken);
            return phone is null ? Results.NotFound() : Results.Ok(phone);
        }).RequireRateLimiting(ListingVisitorRateLimiting.PhoneRevealPolicy);
    }

    private static Guid? UserId(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true ? user.GetUserId() : null;
}
