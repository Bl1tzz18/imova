using Imova.Application.Features.Auth.Register;
using MediatR;

namespace Imova.Api.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static void MapRegister(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/register", async (RegisterCommand command, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(command, cancellationToken)));
    }
}
