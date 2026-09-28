using Imova.Api.Common;
using Imova.Application.Features.Auth.ForgotPassword;
using MediatR;

namespace Imova.Api.Features.Auth.ForgotPassword;

public static class ForgotPasswordEndpoint
{
    public static void MapForgotPassword(this IEndpointRouteBuilder app)
    {
        // Always 204 — whether or not the email has an account (see ForgotPasswordCommand).
        app.MapPost("/api/v1/auth/forgot-password", async (ForgotPasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
