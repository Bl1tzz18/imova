using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Invitations;

// The invited person's side. An invitation is found either by the token from its link (holding the
// link is proof of the inbox) or by its id from "my invitations" (then the account's email must be
// confirmed — otherwise anyone could register with someone else's address and see their invites).
// Accepting needs an account whose email is the invited one, ignoring case.

public record GetInvitationByTokenQuery(string Token) : IRequest<InvitationDto?>;

public class GetInvitationByTokenHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<GetInvitationByTokenQuery, InvitationDto?>
{
    public async Task<InvitationDto?> Handle(GetInvitationByTokenQuery request, CancellationToken cancellationToken)
    {
        var hash = AgencyInvitationTokens.Hash(request.Token);
        var invitation = await dbContext.AgencyInvitations.AsNoTracking().FirstOrDefaultAsync(i => i.TokenHash == hash, cancellationToken);
        return invitation is null
            ? null
            : await InvitationResponses.ToDtoAsync(dbContext, blobStorageService, invitation, timeProvider.GetUtcNow(), cancellationToken);
    }
}

public record GetMyInvitationsQuery(Guid UserId) : IRequest<List<InvitationDto>>;

public class GetMyInvitationsHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<GetMyInvitationsQuery, List<InvitationDto>>
{
    public async Task<List<InvitationDto>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return [];
        }

        var now = timeProvider.GetUtcNow();
        var address = AgencyInvitation.NormalizeEmail(user.Email);
        var invitations = await dbContext.AgencyInvitations.AsNoTracking()
            .Where(i => i.Email == address && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.LastSentAt)
            .ToListAsync(cancellationToken);

        var result = new List<InvitationDto>(invitations.Count);
        foreach (var invitation in invitations)
        {
            if (await InvitationResponses.ToDtoAsync(dbContext, blobStorageService, invitation, now, cancellationToken) is { } dto)
            {
                result.Add(dto);
            }
        }

        return result;
    }
}

// Exactly one of Token and InvitationId.
public record AcceptInvitationCommand(string? Token, Guid? InvitationId, Guid UserId) : IRequest<InvitationDto?>;

public class AcceptInvitationValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationValidator()
    {
        RuleFor(c => c).Must(c => string.IsNullOrEmpty(c.Token) != (c.InvitationId is null))
            .WithMessage("Give either the invitation's token or its id.");
    }
}

public class AcceptInvitationHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<AcceptInvitationCommand, InvitationDto?>
{
    public async Task<InvitationDto?> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        var invitation = await InvitationResponses.FindAsync(dbContext, request.Token, request.InvitationId, user, cancellationToken);
        if (invitation is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        InvitationResponses.EnsurePending(invitation, now);
        InvitationResponses.EnsureForThisAccount(invitation, user);

        var agency = await AgencyDtoLoader.FindAsync(dbContext, invitation.AgencyId, cancellationToken);
        if (agency is null)
        {
            return null;
        }

        if (agency.IsMember(user.Id))
        {
            throw new ConflictException("You are already a member of this agency.", ErrorCodes.AgencyAlreadyMember);
        }

        agency.AddMember(user.Id, invitation.Role, now);
        invitation.Accept(user.Id, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await InvitationResponses.ToDtoAsync(dbContext, blobStorageService, invitation, now, cancellationToken);
    }
}

// Exactly one of Token and InvitationId; declining by token needs no account (the link is enough).
public record DeclineInvitationCommand(string? Token, Guid? InvitationId, Guid? UserId) : IRequest<bool>;

public class DeclineInvitationHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<DeclineInvitationCommand, bool>
{
    public async Task<bool> Handle(DeclineInvitationCommand request, CancellationToken cancellationToken)
    {
        var user = request.UserId is { } userId
            ? await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            : null;

        var invitation = await InvitationResponses.FindAsync(dbContext, request.Token, request.InvitationId, user, cancellationToken);
        if (invitation is null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        InvitationResponses.EnsurePending(invitation, now);
        invitation.Decline(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class InvitationResponses
{
    // By token: whoever holds the link. By id: only an invitation to the account's own, confirmed email.
    public static async Task<AgencyInvitation?> FindAsync(
        IApplicationDbContext dbContext, string? token, Guid? invitationId, ApplicationUser? user, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(token))
        {
            var hash = AgencyInvitationTokens.Hash(token);
            return await dbContext.AgencyInvitations.FirstOrDefaultAsync(i => i.TokenHash == hash, cancellationToken);
        }

        if (invitationId is not { } id || user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        var address = AgencyInvitation.NormalizeEmail(user.Email);
        return await dbContext.AgencyInvitations.FirstOrDefaultAsync(i => i.Id == id && i.Email == address, cancellationToken);
    }

    public static void EnsurePending(AgencyInvitation invitation, DateTimeOffset now)
    {
        switch (invitation.StatusAt(now))
        {
            case AgencyInvitationStatus.Pending:
                return;
            case AgencyInvitationStatus.Expired:
                throw new ValidationException(
                    [CodedFailure.Of("Token", "This invitation has expired. Ask the agency to send it again.", ErrorCodes.AgencyInvitationExpired)]);
            default:
                throw new ValidationException(
                    [CodedFailure.Of("Token", "This invitation is no longer valid.", ErrorCodes.AgencyInvitationClosed)]);
        }
    }

    public static void EnsureForThisAccount(AgencyInvitation invitation, ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email) || AgencyInvitation.NormalizeEmail(user.Email) != invitation.Email)
        {
            throw new ForbiddenAccessException(
                "This invitation was sent to another email address. Sign in with that account to accept it.",
                ErrorCodes.AgencyInvitationWrongAccount);
        }
    }

    public static async Task<InvitationDto?> ToDtoAsync(
        IApplicationDbContext dbContext, IBlobStorageService blobStorageService, AgencyInvitation invitation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().FirstOrDefaultAsync(a => a.Id == invitation.AgencyId, cancellationToken);
        if (agency is null)
        {
            return null;
        }

        var inviterName = invitation.InvitedByUserId is null
            ? null
            : await AgencyInvitationRules.DisplayNameAsync(dbContext, invitation.InvitedByUserId, cancellationToken);
        return AgencyInvitationRules.ToRecipientDto(invitation, agency, inviterName, blobStorageService, now);
    }
}
