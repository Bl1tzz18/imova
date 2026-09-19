using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Media.DeleteMedia;
using MediatR;

namespace Imova.Api.Features.Media.DeleteMedia;

public static class DeleteMediaEndpoint
{
    public static void MapDeleteMedia(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/properties/{propertyId:guid}/media/{mediaId:guid}", async (
            Guid propertyId,
            Guid mediaId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteMediaCommand(propertyId, mediaId, user.GetUserId(), user.IsInRole(Roles.Admin));
            var deleted = await sender.Send(command, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();
    }
}
