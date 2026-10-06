using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Domain.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.Members;

// Promotes, demotes or (for oneself) steps down. Ownership is handed over by making someone else an
// Owner, then stepping down. False (404) when the agency or the member isn't there for the caller.
public record ChangeMemberRoleCommand(Guid AgencyId, Guid ActorId, bool IsAdmin, Guid MemberUserId, AgencyRole Role)
    : IRequest<bool>;

public class ChangeMemberRoleValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleValidator()
    {
        RuleFor(c => c.Role).IsInEnum();
    }
}

public class ChangeMemberRoleHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ChangeMemberRoleCommand, bool>
{
    public async Task<bool> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.ActorId, request.IsAdmin) || !agency.IsMember(request.MemberUserId))
        {
            return false;
        }

        if (!AgencyAccess.CanChangeRole(agency, request.ActorId, request.IsAdmin, request.MemberUserId, request.Role))
        {
            throw new ForbiddenAccessException();
        }

        try
        {
            agency.ChangeRole(request.MemberUserId, request.Role, timeProvider.GetUtcNow());
        }
        catch (LastOwnerException ex)
        {
            throw AgencyMemberErrors.LastOwner(ex);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class AgencyMemberErrors
{
    public static ValidationException LastOwner(LastOwnerException ex) =>
        new([CodedFailure.Of("Role", ex.Message, ErrorCodes.AgencyLastOwner)]);
}
