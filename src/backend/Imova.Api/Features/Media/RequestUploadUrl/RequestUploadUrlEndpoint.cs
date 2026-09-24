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
            "/api/v1/listings/{listingId:guid}/media/upload-url",
            async (Guid listingId, RequestUploadUrlRequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new RequestUploadUrlCommand(listingId, body.FileExtension), cancellationToken);
                return Results.Ok(result);
            });
    }
}
