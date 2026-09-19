using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.SuspendListing;
using MediatR;

namespace Imova.Api.Features.Properties.SuspendListing;

public static class SuspendListingEndpoint
{
    public static void MapSuspendListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/suspend", async (
            Guid id,
            SuspendListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new SuspendListingCommand(id, user.IsInRole(Roles.Admin), request.Reason);
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
