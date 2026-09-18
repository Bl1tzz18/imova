using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Auth.UpdateProfile;
using MediatR;

namespace Imova.Api.Features.Auth.UpdateProfile;

public static class UpdateProfileEndpoint
{
    public static void MapUpdateProfile(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/auth/profile", async (
            UpdateProfileRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProfileCommand(user.GetUserId(), request.DisplayName, request.PhoneNumber);
            return Results.Ok(await sender.Send(command, cancellationToken));
        }).RequireAuthorization();
    }
}

public record UpdateProfileRequest(string? DisplayName, string PhoneNumber);
