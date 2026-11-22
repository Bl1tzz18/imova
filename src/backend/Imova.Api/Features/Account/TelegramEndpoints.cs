using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Telegram;
using MediatR;

namespace Imova.Api.Features.Account;

// The account's Telegram connection, from the "Notificări" tab: whether it's connected, a fresh
// t.me link to connect (one-time code, 15 minutes), and disconnecting. The bot itself runs in the Worker.
public static class TelegramEndpoints
{
    public static void MapTelegramEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/telegram", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
                Results.Ok(await sender.Send(new GetTelegramConnectionQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        // 409 telegram.unavailable when the site has no bot configured.
        app.MapPost("/api/v1/users/me/telegram/link", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
                Results.Ok(await sender.Send(new StartTelegramLinkCommand(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        app.MapDelete("/api/v1/users/me/telegram", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DisconnectTelegramCommand(user.GetUserId()), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization();
    }
}
