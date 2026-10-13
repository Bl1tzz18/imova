using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Agencies.Logos;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Invitations;

// Limits on sending invitations, all counted in the database (so they hold across API instances):
// - at most MaxPending open, unexpired invitations per agency (400 agency.tooManyInvitations);
// - at most MaxSendsPerHour invitations sent per agency per hour — an invitation sent twice in the
//   hour counts once, so each one also waits ResendCooldown between sends (429), which caps the
//   emails to one address at 6 an hour.
public static class AgencyInvitationRules
{
    public const int MaxPending = 50;
    public const int MaxSendsPerHour = 20;
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(10);

    public static async Task EnsureCanSendAsync(
        IApplicationDbContext dbContext, Guid agencyId, AgencyInvitation? resending, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (resending is not null && resending.LastSentAt > now - ResendCooldown)
        {
            throw new TooManyRequestsException(
                $"This invitation was sent less than {ResendCooldown.TotalMinutes:0} minutes ago.",
                ErrorCodes.AgencyInvitationResendTooSoon,
                CodedFailure.Params(("minutes", (int)ResendCooldown.TotalMinutes)));
        }

        var hourAgo = now.AddHours(-1);
        var sentLastHour = await dbContext.AgencyInvitations.CountAsync(
            i => i.AgencyId == agencyId && i.LastSentAt > hourAgo, cancellationToken);
        if (sentLastHour >= MaxSendsPerHour)
        {
            throw new TooManyRequestsException(
                $"An agency can send at most {MaxSendsPerHour} invitations an hour.",
                ErrorCodes.AgencyInvitationRateLimit,
                CodedFailure.Params(("max", MaxSendsPerHour)));
        }

        if (resending is null)
        {
            var pending = await dbContext.AgencyInvitations.CountAsync(
                i => i.AgencyId == agencyId && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null && i.ExpiresAt > now,
                cancellationToken);
            if (pending >= MaxPending)
            {
                throw new ValidationException(
                [
                    CodedFailure.Of(
                        "Email",
                        $"An agency can have at most {MaxPending} invitations waiting.",
                        ErrorCodes.AgencyTooManyInvitations,
                        CodedFailure.Params(("max", MaxPending))),
                ]);
            }
        }
    }

    // Best-effort: the invitation (already saved) stays either way. A failed send is recorded on it
    // (EmailFailedAt) so the agency's members list can say so and offer to resend; a later send that
    // goes through clears it.
    public static async Task SendEmailAsync(
        IApplicationDbContext dbContext,
        AgencyInvitationEmail email,
        ILogger logger,
        AgencyInvitation invitation,
        string token,
        Guid? inviterId,
        string inviterName,
        string agencyName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            var language = await LanguageAsync(dbContext, invitation.Email, inviterId, cancellationToken);
            await email.SendAsync(invitation.Email, inviterName, agencyName, invitation.Role, token, language, cancellationToken);
            invitation.RecordEmailSent();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send agency invitation {InvitationId}.", invitation.Id);
            invitation.RecordEmailFailed(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // The invitation is written in the invitee's language when the address already has an account;
    // otherwise there's nobody behind it yet, so in the language of whoever invited (Romanian when
    // that's unknown).
    public static async Task<string> LanguageAsync(
        IApplicationDbContext dbContext, string inviteeEmail, Guid? inviterId, CancellationToken cancellationToken)
    {
        var normalized = inviteeEmail.Trim().ToUpperInvariant();
        var invitee = await dbContext.Users.AsNoTracking()
            .Where(u => u.NormalizedEmail == normalized)
            .Select(u => u.PreferredLanguage)
            .FirstOrDefaultAsync(cancellationToken);
        if (invitee is not null)
        {
            return Languages.Normalize(invitee);
        }

        var inviter = inviterId is { } id
            ? await dbContext.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.PreferredLanguage).FirstOrDefaultAsync(cancellationToken)
            : null;
        return Languages.Normalize(inviter);
    }

    public static async Task<string> DisplayNameAsync(IApplicationDbContext dbContext, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id)
        {
            return "IMOVA";
        }

        var user = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.DisplayName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(user?.DisplayName) ? user.DisplayName : user?.Email ?? "IMOVA";
    }

    public static AgencyInvitationDto ToAgencyDto(AgencyInvitation invitation, string? invitedByName, DateTimeOffset now) =>
        new(
            invitation.Id,
            invitation.Email,
            invitation.Role.ToString(),
            invitation.StatusAt(now).ToString(),
            invitation.LastSentAt,
            invitation.ExpiresAt,
            invitedByName,
            invitation.EmailFailedAt);

    public static InvitationDto ToRecipientDto(
        AgencyInvitation invitation, Agency agency, string? invitedByName, IBlobStorageService blobStorageService, DateTimeOffset now) =>
        new(
            invitation.Id,
            agency.Id,
            agency.Name,
            agency.Slug,
            agency.LogoBlobName is { } logo ? blobStorageService.GetPublicUrl(AgencyLogo.ThumbnailBlobName(logo)) : null,
            agency.IsVerified,
            agency.Status == AgencyStatus.Active,
            invitation.Role.ToString(),
            invitedByName,
            invitation.Email,
            invitation.StatusAt(now).ToString(),
            invitation.ExpiresAt);
}
