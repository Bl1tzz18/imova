using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Favorites.GetFavorites;
using MediatR;

namespace Imova.Api.Features.Favorites.GetFavorites;

public static class GetFavoritesEndpoint
{
    public static void MapGetFavorites(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/favorites", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetFavoritesQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();
    }
}
