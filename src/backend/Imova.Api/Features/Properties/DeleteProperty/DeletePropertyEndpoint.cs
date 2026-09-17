using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.DeleteProperty;
using MediatR;

namespace Imova.Api.Features.Properties.DeleteProperty;

public static class DeletePropertyEndpoint
{
    public static void MapDeleteProperty(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/properties/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new DeletePropertyCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var deleted = await sender.Send(command, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();
    }
}
