using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Invitations;

// Emails an invitation to join as an Admin or Agent. Someone already a member → 409. An address
// that already has an open invitation gets it again (new link, new role) instead of a second one.
// Null (404) when the caller may not see the agency.
public record InviteMemberCommand(Guid AgencyId, Guid ActorId, bool IsAdmin, string Email, AgencyRole Role)
    : IRequest<AgencyInvitationDto?>;

public class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(Agency.MaxEmailLength);
        RuleFor(c => c.Role).Must(r => r is AgencyRole.Admin or AgencyRole.Agent)
            .WithMessage("An invitation is for an Admin or an Agent.");
    }
}

public class InviteMemberHandler(
    IApplicationDbContext dbContext,
    AgencyInvitationEmail email,
    TimeProvider timeProvider,
    ILogger<InviteMemberHandler> logger) : IRequestHandler<InviteMemberCommand, AgencyInvitationDto?>
{
    public async Task<AgencyInvitationDto?> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.ActorId, request.IsAdmin))
        {
            return null;
        }

        if (!AgencyAccess.CanInvite(agency, request.ActorId, request.IsAdmin, request.Role))
        {
            throw new ForbiddenAccessException();
        }

        var address = AgencyInvitation.NormalizeEmail(request.Email);
        var memberIds = agency.Members.Select(m => m.UserId).ToList();
        if (await dbContext.Users.AnyAsync(u => memberIds.Contains(u.Id) && u.Email != null && u.Email.ToLower() == address, cancellationToken))
        {
            throw new ConflictException("That person is already a member of this agency.", ErrorCodes.AgencyAlreadyMember);
        }

        var now = timeProvider.GetUtcNow();
        var open = await dbContext.AgencyInvitations.FirstOrDefaultAsync(
            i => i.AgencyId == agency.Id && i.Email == address && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null,
            cancellationToken);
        await AgencyInvitationRules.EnsureCanSendAsync(dbContext, agency.Id, open, now, cancellationToken);

        var (token, hash) = AgencyInvitationTokens.New();
        var invitation = open ?? AgencyInvitation.Create(agency.Id, address, request.Role, hash, request.ActorId, now);
        if (open is null)
        {
            dbContext.AgencyInvitations.Add(invitation);
        }
        else
        {
            open.Resend(hash, request.Role, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var inviterName = await AgencyInvitationRules.DisplayNameAsync(dbContext, request.ActorId, cancellationToken);
        await AgencyInvitationRules.SendEmailAsync(dbContext, email, logger, invitation, token, inviterName, agency.Name, now, cancellationToken);
        return AgencyInvitationRules.ToAgencyDto(invitation, inviterName, now);
    }
}
