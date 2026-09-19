using Imova.Api.Features.Media.ConfirmMediaUpload;
using Imova.Api.Features.Media.DeleteMedia;
using Imova.Api.Features.Media.RequestUploadUrl;

namespace Imova.Api.Features.Media;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRequestUploadUrl();
        app.MapConfirmMediaUpload();
        app.MapDeleteMedia();
    }
}
