using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
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
            async (Guid listingId, RequestUploadUrlRequestBody body, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new RequestUploadUrlCommand(listingId, body.FileExtension, user.GetUserId(), user.IsInRole(Roles.Admin));
                return Results.Ok(await sender.Send(command, cancellationToken));
            }).RequireAuthorization();
    }
}
