using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Auth.GetProfile;
using MediatR;

namespace Imova.Api.Features.Auth.GetProfile;

public static class GetProfileEndpoint
{
    public static void MapGetProfile(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/auth/me", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetProfileQuery(user.GetUserId()), cancellationToken))).RequireAuthorization();
    }
}
