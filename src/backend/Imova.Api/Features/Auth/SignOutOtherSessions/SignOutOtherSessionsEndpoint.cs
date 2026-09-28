using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Auth.SignOutOtherSessions;
using MediatR;

namespace Imova.Api.Features.Auth.SignOutOtherSessions;

public static class SignOutOtherSessionsEndpoint
{
    public static void MapSignOutOtherSessions(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/sign-out-other-sessions", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new SignOutOtherSessionsCommand(user.GetUserId(), user.GetSessionId()), cancellationToken)))
            .RequireAuthorization();
    }
}
