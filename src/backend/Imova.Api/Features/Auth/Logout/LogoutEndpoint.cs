using Imova.Application.Features.Auth.Logout;
using MediatR;

namespace Imova.Api.Features.Auth.Logout;

public static class LogoutEndpoint
{
    // Anonymous: signing out must work even when the login token has already run out.
    public static void MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/logout", async (LogoutCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        });
    }
}
