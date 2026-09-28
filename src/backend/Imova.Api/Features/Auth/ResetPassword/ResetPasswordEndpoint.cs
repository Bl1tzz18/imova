using Imova.Api.Common;
using Imova.Application.Features.Auth.ResetPassword;
using MediatR;

namespace Imova.Api.Features.Auth.ResetPassword;

public static class ResetPasswordEndpoint
{
    public static void MapResetPassword(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/reset-password", async (ResetPasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
