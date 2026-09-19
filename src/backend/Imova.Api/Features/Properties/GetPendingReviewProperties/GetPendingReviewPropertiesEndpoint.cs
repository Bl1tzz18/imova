using System.Security.Claims;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.GetPendingReviewProperties;
using MediatR;

namespace Imova.Api.Features.Properties.GetPendingReviewProperties;

public static class GetPendingReviewPropertiesEndpoint
{
    public static void MapGetPendingReviewProperties(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/admin/properties/pending-review", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new GetPendingReviewPropertiesQuery(user.IsInRole(Roles.Admin), page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
