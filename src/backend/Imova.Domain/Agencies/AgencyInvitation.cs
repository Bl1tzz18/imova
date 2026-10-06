using Imova.Domain.Common;

namespace Imova.Domain.Agencies;

public enum AgencyInvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Revoked = 4,
    Expired = 5,
}

// An emailed invitation to join an agency as an Admin or Agent (never as an Owner — that's a
// promotion of an existing member). Only a hash of the link's token is stored. It works for 7 days
// from the last time it was sent; resending sends a new token (the old link stops working).
// Accepting, declining and revoking each end it for good.
public sealed class AgencyInvitation : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    // For EF Core materialization only.
    private AgencyInvitation()
        : base(Guid.Empty)
    {
        Email = null!;
        TokenHash = null!;
    }

    private AgencyInvitation(Guid id, Guid agencyId, string email, AgencyRole role, string tokenHash, Guid? invitedByUserId, DateTimeOffset now)
        : base(id)
    {
        AgencyId = agencyId;
        Email = email;
        Role = role;
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        CreatedAt = now;
        LastSentAt = now;
        ExpiresAt = now + Lifetime;
    }

    public Guid AgencyId { get; private set; }

    // Lower case — matched against the account's email ignoring case.
    public string Email { get; private set; }

    public AgencyRole Role { get; private set; }

    public string TokenHash { get; private set; }

    // Null once that account is deleted.
    public Guid? InvitedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSentAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public Guid? AcceptedByUserId { get; private set; }

    public DateTimeOffset? DeclinedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    // When sending its latest email failed (cleared by the next one that goes out) — the agency's
    // members list shows it, with a resend button.
    public DateTimeOffset? EmailFailedAt { get; private set; }

    public void RecordEmailFailed(DateTimeOffset now) => EmailFailedAt = now;

    public void RecordEmailSent() => EmailFailedAt = null;

    // Still open: not accepted, declined or revoked (it may have expired — resending revives it).
    public bool IsOpen => AcceptedAt is null && DeclinedAt is null && RevokedAt is null;

    public static AgencyInvitation Create(
        Guid agencyId, string email, AgencyRole role, string tokenHash, Guid? invitedByUserId, DateTimeOffset now)
    {
        EnsureInvitable(role);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("An invitation needs an email address.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("An invitation needs its token's hash.", nameof(tokenHash));
        }

        return new AgencyInvitation(Guid.NewGuid(), agencyId, NormalizeEmail(email), role, tokenHash, invitedByUserId, now);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public AgencyInvitationStatus StatusAt(DateTimeOffset now) =>
        AcceptedAt is not null ? AgencyInvitationStatus.Accepted
        : DeclinedAt is not null ? AgencyInvitationStatus.Declined
        : RevokedAt is not null ? AgencyInvitationStatus.Revoked
        : ExpiresAt <= now ? AgencyInvitationStatus.Expired
        : AgencyInvitationStatus.Pending;

    // A new link (and role) for an invitation that's still open, expired or not.
    public void Resend(string tokenHash, AgencyRole role, DateTimeOffset now)
    {
        EnsureInvitable(role);
        EnsureOpen();
        TokenHash = tokenHash;
        Role = role;
        LastSentAt = now;
        ExpiresAt = now + Lifetime;
    }

    public void Accept(Guid userId, DateTimeOffset now)
    {
        EnsurePending(now);
        AcceptedAt = now;
        AcceptedByUserId = userId;
    }

    public void Decline(DateTimeOffset now)
    {
        EnsurePending(now);
        DeclinedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        EnsureOpen();
        RevokedAt = now;
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("This invitation was already accepted, declined or revoked.");
        }
    }

    private void EnsurePending(DateTimeOffset now)
    {
        if (StatusAt(now) != AgencyInvitationStatus.Pending)
        {
            throw new InvalidOperationException("This invitation is no longer valid.");
        }
    }

    private static void EnsureInvitable(AgencyRole role)
    {
        if (role is not (AgencyRole.Admin or AgencyRole.Agent))
        {
            throw new ArgumentException("An invitation is for an Admin or an Agent; Owners are promoted, not invited.", nameof(role));
        }
    }
}
