using System.Security.Claims;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.SuspendListing;
using MediatR;

namespace Imova.Api.Features.Listings.SuspendListing;

public record SuspendListingRequest(string Reason);

public static class SuspendListingEndpoint
{
    public static void MapSuspendListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/listings/{id:guid}/suspend", async (
            Guid id,
            SuspendListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var listing = await sender.Send(new SuspendListingCommand(id, user.IsInRole(Roles.Admin), request.Reason), cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(listing);
        }).RequireAuthorization();
    }
}
