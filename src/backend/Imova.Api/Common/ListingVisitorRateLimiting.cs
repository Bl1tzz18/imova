using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Imova.Api.Common;

// Bound from "RateLimiting:ListingView" and "RateLimiting:PhoneReveal".
public class ListingVisitorRateLimitOptions
{
    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}

// Per client IP (the visitor's, forwarded by the web app — see AuthRateLimiting): how often anyone
// may record views (inflating a counter takes many ids from many places) and ask for phone numbers
// (the brake on harvesting them — a person looking for a home asks for a few, not hundreds).
public static class ListingVisitorRateLimiting
{
    public const string ViewPolicy = "listingView";
    public const string PhoneRevealPolicy = "phoneReveal";

    public static void AddListingVisitorPolicies(this RateLimiterOptions limiter, IConfiguration configuration)
    {
        Add(limiter, ViewPolicy, Options(configuration, "RateLimiting:ListingView", permits: 120, windowSeconds: 60));
        Add(limiter, PhoneRevealPolicy, Options(configuration, "RateLimiting:PhoneReveal", permits: 30, windowSeconds: 600));
    }

    private static ListingVisitorRateLimitOptions Options(IConfiguration configuration, string section, int permits, int windowSeconds)
    {
        var options = new ListingVisitorRateLimitOptions { PermitLimit = permits, WindowSeconds = windowSeconds };
        configuration.GetSection(section).Bind(options);
        return options;
    }

    private static void Add(RateLimiterOptions limiter, string policy, ListingVisitorRateLimitOptions options) =>
        limiter.AddPolicy(policy, httpContext => options.Enabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = 0,
                })
            : RateLimitPartition.GetNoLimiter("disabled"));
}
