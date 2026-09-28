using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.SavedSearches.CreateSavedSearch;
using Imova.Application.Features.SavedSearches.ManageSavedSearches;
using Imova.Application.Features.SavedSearches.Unsubscribe;
using Imova.Domain.SavedSearches;
using MediatR;

namespace Imova.Api.Features.SavedSearches;

// A user's saved searches (see SavedSearch). Everything is scoped to the caller — another user's
// saved search is a 404 — except /unsubscribe, the tokened one-click link from alert emails.
public static class SavedSearchEndpoints
{
    public static void MapSavedSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/saved-searches");

        group.MapGet("", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetSavedSearchesQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        group.MapPost("", async (CreateSavedSearchRequest body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            var created = await sender.Send(
                new CreateSavedSearchCommand(user.GetUserId(), body.Name, body.QueryString, body.AlertFrequency), cancellationToken);
            return Results.Ok(created);
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, UpdateSavedSearchRequest body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            var updated = await sender.Send(new UpdateSavedSearchCommand(user.GetUserId(), id, body.Name, body.AlertFrequency), cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(new DeleteSavedSearchCommand(user.GetUserId(), id), cancellationToken)
                ? Results.NoContent()
                : Results.NotFound())
            .RequireAuthorization();

        // Opening a saved search: resets its "new since your last visit" count; returns it so the
        // caller knows which /search query to open.
        group.MapPost("/{id:guid}/viewed", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            var viewed = await sender.Send(new MarkSavedSearchViewedCommand(user.GetUserId(), id), cancellationToken);
            return viewed is null ? Results.NotFound() : Results.Ok(viewed);
        }).RequireAuthorization();

        group.MapPost("/unsubscribe", async (UnsubscribeSavedSearchCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireRateLimiting(AuthRateLimiting.Policy);
    }
}

public record CreateSavedSearchRequest(string Name, string QueryString, AlertFrequency AlertFrequency);

public record UpdateSavedSearchRequest(string Name, AlertFrequency AlertFrequency);
