using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Invitations;

// The agency's side of its invitations: the open ones (waiting or expired), resending and revoking.
// For whoever may invite that role (AgencyAccess.CanInvite). Null (404) when the caller may not see
// the agency, or the invitation isn't one of its open ones.

public record GetAgencyInvitationsQuery(Guid AgencyId, Guid ActorId, bool IsAdmin) : IRequest<List<AgencyInvitationDto>?>;

public class GetAgencyInvitationsHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetAgencyInvitationsQuery, List<AgencyInvitationDto>?>
{
    public async Task<List<AgencyInvitationDto>?> Handle(GetAgencyInvitationsQuery request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.ActorId, request.IsAdmin))
        {
            return null;
        }

        if (!AgencyAccess.CanManageMembers(agency, request.ActorId, request.IsAdmin))
        {
            throw new ForbiddenAccessException();
        }

        var invitations = await dbContext.AgencyInvitations.AsNoTracking()
            .Where(i => i.AgencyId == agency.Id && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null)
            .OrderByDescending(i => i.LastSentAt)
            .ToListAsync(cancellationToken);

        var inviterIds = invitations.Select(i => i.InvitedByUserId).OfType<Guid>().Distinct().ToList();
        var inviters = await dbContext.Users.AsNoTracking()
            .Where(u => inviterIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.DisplayName ?? u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var now = timeProvider.GetUtcNow();
        return invitations
            .Select(i => AgencyInvitationRules.ToAgencyDto(i, i.InvitedByUserId is { } by ? inviters.GetValueOrDefault(by) : null, now))
            .ToList();
    }
}

public record ResendAgencyInvitationCommand(Guid AgencyId, Guid ActorId, bool IsAdmin, Guid InvitationId) : IRequest<AgencyInvitationDto?>;

public class ResendAgencyInvitationHandler(
    IApplicationDbContext dbContext,
    AgencyInvitationEmail email,
    TimeProvider timeProvider,
    ILogger<ResendAgencyInvitationHandler> logger) : IRequestHandler<ResendAgencyInvitationCommand, AgencyInvitationDto?>
{
    public async Task<AgencyInvitationDto?> Handle(ResendAgencyInvitationCommand request, CancellationToken cancellationToken)
    {
        var (agency, invitation) = await ManageAgencyInvitations.FindOpenAsync(
            dbContext, request.AgencyId, request.ActorId, request.IsAdmin, request.InvitationId, cancellationToken);
        if (agency is null || invitation is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        await AgencyInvitationRules.EnsureCanSendAsync(dbContext, agency.Id, invitation, now, cancellationToken);

        var (token, hash) = AgencyInvitationTokens.New();
        invitation.Resend(hash, invitation.Role, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        var inviterName = await AgencyInvitationRules.DisplayNameAsync(dbContext, request.ActorId, cancellationToken);
        await AgencyInvitationRules.SendEmailAsync(dbContext, email, logger, invitation, token, inviterName, agency.Name, now, cancellationToken);
        return AgencyInvitationRules.ToAgencyDto(invitation, inviterName, now);
    }
}

public record RevokeAgencyInvitationCommand(Guid AgencyId, Guid ActorId, bool IsAdmin, Guid InvitationId) : IRequest<bool>;

public class RevokeAgencyInvitationHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<RevokeAgencyInvitationCommand, bool>
{
    public async Task<bool> Handle(RevokeAgencyInvitationCommand request, CancellationToken cancellationToken)
    {
        var (_, invitation) = await ManageAgencyInvitations.FindOpenAsync(
            dbContext, request.AgencyId, request.ActorId, request.IsAdmin, request.InvitationId, cancellationToken);
        if (invitation is null)
        {
            return false;
        }

        invitation.Revoke(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class ManageAgencyInvitations
{
    // The agency and one of its open invitations, for a caller allowed to invite that role (else 403).
    public static async Task<(Agency? Agency, AgencyInvitation? Invitation)> FindOpenAsync(
        IApplicationDbContext dbContext, Guid agencyId, Guid actorId, bool isAdmin, Guid invitationId, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, agencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, actorId, isAdmin))
        {
            return (null, null);
        }

        var invitation = await dbContext.AgencyInvitations.FirstOrDefaultAsync(
            i => i.Id == invitationId && i.AgencyId == agencyId && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null,
            cancellationToken);
        if (invitation is null)
        {
            return (agency, null);
        }

        if (!AgencyAccess.CanInvite(agency, actorId, isAdmin, invitation.Role))
        {
            throw new ForbiddenAccessException();
        }

        return (agency, invitation);
    }
}
