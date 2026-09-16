using Imova.Application.Features.Media.RequestUploadUrl;
using MediatR;

namespace Imova.Api.Features.Media.RequestUploadUrl;

// FileExtension includes the leading dot, e.g. ".jpg".
public record RequestUploadUrlRequestBody(string FileExtension);

public static class RequestUploadUrlEndpoint
{
    public static void MapRequestUploadUrl(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/properties/{propertyId:guid}/media/upload-url",
            async (Guid propertyId, RequestUploadUrlRequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new RequestUploadUrlCommand(propertyId, body.FileExtension), cancellationToken);
                return Results.Ok(result);
            });
    }
}
