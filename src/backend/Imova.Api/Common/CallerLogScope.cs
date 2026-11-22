using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Imova.Api.Common;

// Every log line written while handling a signed-in request carries the caller's account id as
// "CallerId" — that's how a business event (AppEvents) says which admin approved a listing, even
// when the command itself only holds the listing. An id only, never the email.
public static class CallerLogScope
{
    public const string Key = "CallerId";

    public static Guid? CallerId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;

    public static IApplicationBuilder UseCallerLogScope(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (CallerId(context.User) is not { } callerId)
            {
                await next(context);
                return;
            }

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CallerLogScope));
            using (logger.BeginScope(new Dictionary<string, object> { [Key] = callerId }))
            {
                await next(context);
            }
        });
}
