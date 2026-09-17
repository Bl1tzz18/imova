using Imova.Infrastructure.Identity;

namespace Imova.Api.Features.Auth.Config;

// Lets the frontend fetch the (non-secret) Google OAuth Client ID at runtime instead of needing
// its own copy of the value in a separate frontend config/env file — GoogleAuthOptions:ClientId in
// appsettings.Development.json is the single place it needs to be set.
public static class AuthConfigEndpoint
{
    public static void MapAuthConfig(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/auth/config", (GoogleAuthOptions options) =>
            Results.Ok(new { googleClientId = options.ClientId }));
    }
}
