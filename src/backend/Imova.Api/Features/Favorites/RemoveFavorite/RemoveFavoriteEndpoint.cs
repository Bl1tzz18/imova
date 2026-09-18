using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Favorites.RemoveFavorite;
using MediatR;

namespace Imova.Api.Features.Favorites.RemoveFavorite;

public static class RemoveFavoriteEndpoint
{
    public static void MapRemoveFavorite(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/properties/{id:guid}/favorite", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(new RemoveFavoriteCommand(user.GetUserId(), id), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
