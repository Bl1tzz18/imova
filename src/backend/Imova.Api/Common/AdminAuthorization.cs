using Imova.Application.Common.Identity;

namespace Imova.Api.Common;

public static class AdminAuthorization
{
    // Signed in *and* in the Admin role — checked by the authorization middleware before the
    // request is bound, validated or handled: an anonymous caller gets 401, anyone else 403, and
    // nothing about the target (does that listing exist? is the body valid?) leaks out. Handlers
    // keep their own IsAdmin check as a second line.
    public static TBuilder RequireAdmin<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
}
