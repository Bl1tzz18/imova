using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Users.RemoveProfilePicture;
using MediatR;

namespace Imova.Api.Features.Users.RemoveProfilePicture;

public static class RemoveProfilePictureEndpoint
{
    public static void MapRemoveProfilePicture(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/users/me/profile-picture", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new RemoveProfilePictureCommand(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();
    }
}
