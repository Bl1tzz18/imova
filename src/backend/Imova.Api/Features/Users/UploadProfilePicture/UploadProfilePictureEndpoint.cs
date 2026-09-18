using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Users.UploadProfilePicture;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Imova.Api.Features.Users.UploadProfilePicture;

public static class UploadProfilePictureEndpoint
{
    public static void MapUploadProfilePicture(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/users/me/profile-picture", async (
            IFormFile file,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            var command = new UploadProfilePictureCommand(user.GetUserId(), buffer.ToArray());
            return Results.Ok(await sender.Send(command, cancellationToken));
        }).RequireAuthorization().DisableAntiforgery();
    }
}
