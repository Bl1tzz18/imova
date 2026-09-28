using Imova.Application.Features.Auth.RefreshSession;
using MediatR;

namespace Imova.Api.Features.Auth.RefreshSession;

public static class RefreshSessionEndpoint
{
    // Anonymous on purpose: it's called exactly when the login token has run out. The refresh token
    // itself (256 random bits, single use) is the credential, so there's nothing to rate-limit.
    public static void MapRefreshSession(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/refresh", async (RefreshSessionCommand command, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(command, cancellationToken)));
    }
}
