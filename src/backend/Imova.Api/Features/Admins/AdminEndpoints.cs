using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Admins.GetAdmins;
using Imova.Application.Features.Admins.GrantAdmin;
using MediatR;

namespace Imova.Api.Features.Admins;

public static class AdminEndpoints
{
    public record GrantAdminRequest(string Email, string? Password);

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/admin/admins", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAdminsQuery(user.GetUserId()), cancellationToken)))
            .RequireAdmin();

        // Role-gated at the route, re-checked against the database, password-confirmed, rate
        // limited per admin, audited and announced by email — see GrantAdminHandler.
        app.MapPost("/api/v1/admin/admins", async (
            GrantAdminRequest request, ClaimsPrincipal user, HttpContext httpContext, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new GrantAdminCommand(user.GetUserId(), request.Email, request.Password, httpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken);
            return Results.NoContent();
        })
            .RequireAdmin()
            .RequireRateLimiting(AuthRateLimiting.AccountPolicy);
    }
}
