using Imova.Application.Features.Media.ConfirmMediaUpload;
using MediatR;

namespace Imova.Api.Features.Media.ConfirmMediaUpload;

public record ConfirmMediaUploadRequestBody(string BlobName);

public static class ConfirmMediaUploadEndpoint
{
    public static void MapConfirmMediaUpload(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/listings/{listingId:guid}/media/confirm",
            async (Guid listingId, ConfirmMediaUploadRequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ConfirmMediaUploadCommand(listingId, body.BlobName), cancellationToken);
                return Results.Ok(result);
            });
    }
}
