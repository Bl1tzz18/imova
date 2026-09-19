using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.SubmitForReview;
using MediatR;

namespace Imova.Api.Features.Properties.SubmitForReview;

public static class SubmitForReviewEndpoint
{
    public static void MapSubmitForReview(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/submit-for-review", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new SubmitForReviewCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
