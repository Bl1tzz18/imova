using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.ApproveListing;
using MediatR;

namespace Imova.Api.Features.Properties.ApproveListing;

public static class ApproveListingEndpoint
{
    public static void MapApproveListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/approve", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new ApproveListingCommand(id, user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
