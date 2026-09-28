using Imova.Api.Common;
using Imova.Application.Features.Auth.ConfirmEmail;
using MediatR;

namespace Imova.Api.Features.Auth.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    public static void MapConfirmEmail(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/confirm-email", async (ConfirmEmailCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
