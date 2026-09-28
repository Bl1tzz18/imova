using System.Threading.RateLimiting;
using Imova.Application.Common;
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
public static class AuthRateLimiting
{
    public const string Policy = "auth";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
            ?? new AuthRateLimitOptions();

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
            {
                forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

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
        });

        return services;
    }
}
