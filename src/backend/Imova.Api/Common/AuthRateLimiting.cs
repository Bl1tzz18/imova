using System.Threading.RateLimiting;
using Imova.Application.Common;
using Imova.Application.Common.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace Imova.Api.Common;

// Bound from "RateLimiting:Auth".
public class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    // Integration tests turn it off (they register hundreds of users from one "IP").
    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}

// Bound from "RateLimiting:Account": per signed-in user, per endpoint — the personal data export
// (heavy: it reads every file of the account) and the account deletion endpoints.
public class AccountRateLimitOptions
{
    public const string SectionName = "RateLimiting:Account";

    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 5;

    public int WindowSeconds { get; set; } = 600;
}

// A per-client-IP fixed window on the anonymous auth endpoints (login, register, Google sign-in,
// forgot/reset password, email confirmation) — slows down password guessing and account/email
// spamming. Per-account protection is separate: Identity lockout (LoginHandler) and
// AuthEmailThrottle.
//
// The client IP: the web app calls this API from its server (server actions), so without help
// every visitor would share the Next.js server's IP and one limit. The web app forwards the
// visitor's IP as X-Forwarded-For, and UseForwardedHeaders honours it only when the request comes
// from a trusted proxy — loopback by default, plus the networks in ForwardedHeaders:KnownNetworks
// (compose: the docker network). Anyone else sending X-Forwarded-For is limited by their own IP.
// See ConfigureForwardedHeaders for the hosted (Azure Container Apps) case.
public static class AuthRateLimiting
{
    public const string Policy = "auth";

    // See AccountRateLimitOptions. Needs an authenticated endpoint (partitions by the user id).
    public const string AccountPolicy = "account";

    // Which X-Forwarded-For entries to believe. Locally (compose) the web app is the only proxy:
    // one entry, from the docker network. Behind Azure Container Apps there are two hops: the
    // ingress (Envoy) in front of every app appends the address it saw, so a server action's call
    // arrives as "visitor, web app" from the ingress, and a browser's direct call as "client" (after
    // anything the client sent itself). With ForwardedHeaders:ForwardLimit = 2 and the private
    // ranges as KnownNetworks, the middleware walks back from the right through trusted (private)
    // addresses and stops at the first public one: the visitor in the first case, the real client
    // in the second — a spoofed entry further left is never reached.
    public static void ConfigureForwardedHeaders(ForwardedHeadersOptions forwarded, IConfiguration configuration)
    {
        forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
        {
            forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }

        if (configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") is > 0 and var limit)
        {
            forwarded.ForwardLimit = limit;
        }
    }

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
            ?? new AuthRateLimitOptions();
        var accountOptions = configuration.GetSection(AccountRateLimitOptions.SectionName).Get<AccountRateLimitOptions>()
            ?? new AccountRateLimitOptions();

        services.Configure<ForwardedHeadersOptions>(forwarded => ConfigureForwardedHeaders(forwarded, configuration));

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                // A business event (AppEvents): which limit and which route — many in a row from one
                // place is someone guessing passwords or scraping. No IP, no account email.
                var endpoint = context.HttpContext.GetEndpoint();
                var policy = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "unknown";
                var route = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? context.HttpContext.Request.Path.Value;
                context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(AppEvents.Category)
                    .LogWarning("{Event}: {Policy} on {Method} {Route}", "RateLimited", policy, context.HttpContext.Request.Method, route);

                await Results.Problem(
                        "Too many attempts. Wait a minute and try again.",
                        statusCode: StatusCodes.Status429TooManyRequests,
                        extensions: ProblemCodes.For(ErrorCodes.RateLimited))
                    .ExecuteAsync(context.HttpContext);
            };

            limiter.AddPolicy(Policy, httpContext => options.Enabled
                ? RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = 0,
                    })
                : RateLimitPartition.GetNoLimiter("disabled"));

            limiter.AddPolicy(AccountPolicy, httpContext => accountOptions.Enabled
                ? RateLimitPartition.GetFixedWindowLimiter(
                    $"{httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value}:{httpContext.Request.Method}:{httpContext.Request.Path}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = accountOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(accountOptions.WindowSeconds),
                        QueueLimit = 0,
                    })
                : RateLimitPartition.GetNoLimiter("disabled"));

            limiter.AddListingVisitorPolicies(configuration);
        });

        return services;
    }
}
