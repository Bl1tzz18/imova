using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
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
            async (Guid listingId, ConfirmMediaUploadRequestBody body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new ConfirmMediaUploadCommand(listingId, body.BlobName, user.GetUserId(), user.IsInRole(Roles.Admin));
                return Results.Ok(await sender.Send(command, cancellationToken));
            }).RequireAuthorization();
    }
}
