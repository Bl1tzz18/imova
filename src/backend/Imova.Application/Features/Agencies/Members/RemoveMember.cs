using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.Members;

// Removes a member, or lets one leave (ActorId == MemberUserId). Their listings under the agency
// stay with it, handed to ReassignTo — an Owner or Admin who stays — or, when that's left out, to
// AgencyListingReassignment.DefaultHeir. All in one save. False (404) when the agency or the
// member isn't there for the caller.
public record RemoveMemberCommand(Guid AgencyId, Guid ActorId, bool IsAdmin, Guid MemberUserId, Guid? ReassignTo)
    : IRequest<bool>;

public class RemoveMemberHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<RemoveMemberCommand, bool>
{
    public async Task<bool> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.ActorId, request.IsAdmin) || !agency.IsMember(request.MemberUserId))
        {
            return false;
        }

        if (!AgencyAccess.CanRemoveMember(agency, request.ActorId, request.IsAdmin, request.MemberUserId))
        {
            throw new ForbiddenAccessException();
        }

        // The last-Owner rule first: their listings mustn't move if they can't go.
        if (agency.RoleOf(request.MemberUserId) is AgencyRole.Owner && agency.OwnerCount == 1)
        {
            throw AgencyMemberErrors.LastOwner(new LastOwnerException());
        }

        var heir = request.ReassignTo ?? AgencyListingReassignment.DefaultHeir(agency, request.MemberUserId, request.ActorId);
        await AgencyListingReassignment.MoveAsync(dbContext, agency, request.MemberUserId, heir, cancellationToken);

        agency.RemoveMember(request.MemberUserId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
