using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Favorites.SaveFavorite;
using MediatR;

namespace Imova.Api.Features.Favorites.SaveFavorite;

public static class SaveFavoriteEndpoint
{
    public static void MapSaveFavorite(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/listings/{id:guid}/favorite", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var saved = await sender.Send(new SaveFavoriteCommand(user.GetUserId(), id), cancellationToken);
            return saved ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization();
    }
}
