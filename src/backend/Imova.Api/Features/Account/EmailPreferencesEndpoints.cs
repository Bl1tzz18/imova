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

    public record UpdatePreferredLanguageRequest(string Language);

    public static void MapEmailPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/email-preferences", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetEmailPreferencesQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        app.MapPut("/api/v1/users/me/email-preferences", async (
                UpdateEmailPreferencesRequest body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new UpdateEmailPreferencesCommand(user.GetUserId(), body.FavoriteUpdates), cancellationToken)))
            .RequireAuthorization();

        // The language every email to the account is written in — the web app sends the site's
        // language here when a signed-in user switches it (204).
        app.MapPut("/api/v1/users/me/language", async (
                UpdatePreferredLanguageRequest body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new UpdatePreferredLanguageCommand(user.GetUserId(), body.Language), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization();

        app.MapPost("/api/v1/favorites/alerts/unsubscribe", async (
                UnsubscribeFavoriteAlertsCommand command, ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(command, cancellationToken) ? Results.NoContent() : Results.NotFound())
            .RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
