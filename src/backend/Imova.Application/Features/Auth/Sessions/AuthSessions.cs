using System.Security.Cryptography;
using System.Text;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Auth.Sessions;

// Login sessions: a short-lived login token (JWT, Jwt:ExpiryMinutes = 15) plus a refresh token that
// the web app trades for a new pair whenever the login token runs out (POST /auth/refresh).
// - Remembered sessions last AuthSessionOptions.RememberedDays without a visit, others
//   AuthSessionOptions.SessionHours; every session ends after AuthSessionOptions.MaxDays
//   regardless.
// - Each refresh replaces the token (rotation). A replaced token used again after the grace period
//   means two parties hold it — the whole session is revoked.
// - Tokens die with the account's security stamp (password change/reset, "sign out other
//   devices"), like the login tokens themselves (SessionStamp).
public class AuthSessions(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator,
    AuthSessionOptions options,
    TimeProvider timeProvider)
{
    public const string SessionIdClaim = "sid";

    private const string Expired = "Your session has ended. Please sign in again.";

    public async Task<SessionTokenDto> StartAsync(ApplicationUser user, bool persistent, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await IssueAsync(user, Guid.NewGuid(), persistent, now.AddDays(options.MaxDays), now, cancellationToken);
    }

    // After the caller's own action changed the security stamp (password change, "sign out other
    // devices"): every other session is now dead, and this one continues — same session id,
    // remember-me choice and absolute limit — with tokens made from the new stamp.
    public async Task<SessionTokenDto> ContinueAsync(ApplicationUser user, Guid? sessionId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var latest = sessionId is { } id
            ? await dbContext.RefreshTokens
                .Where(t => t.SessionId == id && t.UserId == user.Id && t.RevokedAt == null)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (latest is null || latest.SessionExpiresAt <= now)
        {
            return await StartAsync(user, persistent: false, cancellationToken);
        }

        return await IssueAsync(user, latest.SessionId, latest.Persistent, latest.SessionExpiresAt, now, cancellationToken);
    }

    public async Task<SessionTokenDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var hash = Hash(refreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw SessionEnded();

        if (token.RevokedAt is not null || token.ExpiresAt <= now || token.SessionExpiresAt <= now)
        {
            throw SessionEnded();
        }

        if (token.ReplacedAt is { } replacedAt && now - replacedAt > TimeSpan.FromSeconds(options.ReuseGraceSeconds))
        {
            // Someone is using a token that was already exchanged: either the owner or a thief has
            // a copy. End the session for both.
            await RevokeSessionAsync(token.SessionId, now, cancellationToken);
            throw SessionEnded();
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null || SessionStamp.For(user.SecurityStamp) != token.StampFingerprint)
        {
            throw SessionEnded();
        }

        token.ReplacedAt ??= now;
        return await IssueAsync(user, token.SessionId, token.Persistent, token.SessionExpiresAt, now, cancellationToken);
    }

    // Sign-out. An unknown or already-ended token is fine — the result is the same.
    public async Task EndAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        var sessionId = await dbContext.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.SessionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sessionId is { } id)
        {
            await RevokeSessionAsync(id, timeProvider.GetUtcNow(), cancellationToken);
        }
    }

    public static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private async Task<SessionTokenDto> IssueAsync(
        ApplicationUser user, Guid sessionId, bool persistent, DateTimeOffset sessionExpiresAt, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var idle = persistent ? TimeSpan.FromDays(options.RememberedDays) : TimeSpan.FromHours(options.SessionHours);
        var expiresAt = now + idle < sessionExpiresAt ? now + idle : sessionExpiresAt;

        var refreshToken = Base64Url(RandomNumberGenerator.GetBytes(32));
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = sessionId,
            TokenHash = Hash(refreshToken),
            Persistent = persistent,
            StampFingerprint = SessionStamp.For(user.SecurityStamp),
            CreatedAt = now,
            ExpiresAt = expiresAt,
            SessionExpiresAt = sessionExpiresAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenGenerator.GenerateToken(user, await userManager.GetRolesAsync(user), sessionId);
        return new SessionTokenDto(accessToken.Value, accessToken.ExpiresAt, refreshToken, expiresAt, persistent);
    }

    private async Task RevokeSessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.SessionId == sessionId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AuthenticationFailedException SessionEnded() => new(Expired, ErrorCodes.SessionExpired);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
