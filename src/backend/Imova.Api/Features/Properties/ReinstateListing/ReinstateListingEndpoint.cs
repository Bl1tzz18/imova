using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.ReinstateListing;
using MediatR;

namespace Imova.Api.Features.Properties.ReinstateListing;

public static class ReinstateListingEndpoint
{
    public static void MapReinstateListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/reinstate", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new ReinstateListingCommand(id, user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
