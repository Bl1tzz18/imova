using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Auth.ChangePassword;
using MediatR;

namespace Imova.Api.Features.Auth.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static void MapChangePassword(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/auth/password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangePasswordCommand(user.GetUserId(), request.CurrentPassword, request.NewPassword);
            return Results.Ok(await sender.Send(command, cancellationToken));
        }).RequireAuthorization();
    }
}

public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
