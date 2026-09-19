using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Properties.GetMyProperties;
using MediatR;

namespace Imova.Api.Features.Properties.GetMyProperties;

public static class GetMyPropertiesEndpoint
{
    public static void MapGetMyProperties(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/properties", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMyPropertiesQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();
    }
}
