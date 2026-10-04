using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Account.EmailPreferences;
using Imova.Application.Features.Favorites.Alerts;
using MediatR;

namespace Imova.Api.Features.Account;

// Which optional emails the user gets: read/changed from the account settings, or turned off in
// one click from the link in such an email (tokened, no sign-in).
public static class EmailPreferencesEndpoints
{
    public record UpdateEmailPreferencesRequest(bool FavoriteUpdates);

    public static void MapEmailPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/email-preferences", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetEmailPreferencesQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        app.MapPut("/api/v1/users/me/email-preferences", async (
                UpdateEmailPreferencesRequest body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new UpdateEmailPreferencesCommand(user.GetUserId(), body.FavoriteUpdates), cancellationToken)))
            .RequireAuthorization();

        app.MapPost("/api/v1/favorites/alerts/unsubscribe", async (
                UnsubscribeFavoriteAlertsCommand command, ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(command, cancellationToken) ? Results.NoContent() : Results.NotFound())
            .RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
