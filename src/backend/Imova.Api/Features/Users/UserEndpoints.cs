using Imova.Api.Features.Users.RemoveProfilePicture;
using Imova.Api.Features.Users.UploadProfilePicture;

namespace Imova.Api.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapUploadProfilePicture();
        app.MapRemoveProfilePicture();
    }
}
