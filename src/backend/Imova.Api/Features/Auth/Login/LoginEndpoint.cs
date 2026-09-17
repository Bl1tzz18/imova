using Imova.Application.Features.Auth.Login;
using MediatR;

namespace Imova.Api.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/login", async (LoginCommand command, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(command, cancellationToken)));
    }
}
