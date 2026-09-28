using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Auth.ResendConfirmation;
using MediatR;

namespace Imova.Api.Features.Auth.ResendConfirmation;

public static class ResendConfirmationEndpoint
{
    public static void MapResendConfirmation(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/resend-confirmation", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ResendConfirmationCommand(user.GetUserId()), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
