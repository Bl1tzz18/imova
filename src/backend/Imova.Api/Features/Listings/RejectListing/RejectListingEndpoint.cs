using System.Security.Claims;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.RejectListing;
using MediatR;

namespace Imova.Api.Features.Listings.RejectListing;

public record RejectListingRequest(string Reason);

public static class RejectListingEndpoint
{
    public static void MapRejectListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/listings/{id:guid}/reject", async (
            Guid id,
            RejectListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var listing = await sender.Send(new RejectListingCommand(id, user.IsInRole(Roles.Admin), request.Reason), cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(listing);
        }).RequireAuthorization();
    }
}
