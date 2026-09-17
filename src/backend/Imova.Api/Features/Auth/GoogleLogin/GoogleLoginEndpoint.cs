using Imova.Application.Features.Auth.GoogleLogin;
using MediatR;

namespace Imova.Api.Features.Auth.GoogleLogin;

public static class GoogleLoginEndpoint
{
    public static void MapGoogleLogin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/google", async (GoogleLoginCommand command, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(command, cancellationToken)));
    }
}
