using Imova.Api.Features.Favorites.GetFavorites;
using Imova.Api.Features.Favorites.RemoveFavorite;
using Imova.Api.Features.Favorites.SaveFavorite;

namespace Imova.Api.Features.Favorites;

public static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSaveFavorite();
        app.MapRemoveFavorite();
        app.MapGetFavorites();
    }
}
