using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.DeleteListing;
using MediatR;

namespace Imova.Api.Features.Listings.DeleteListing;

public static class DeleteListingEndpoint
{
    public static void MapDeleteListing(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/listings/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteListingCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var deleted = await sender.Send(command, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();
    }
}
