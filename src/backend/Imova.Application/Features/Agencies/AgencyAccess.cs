using Imova.Application.Common.Exceptions;
using Imova.Domain.Agencies;

namespace Imova.Application.Features.Agencies;

// Who may do what with an agency (the agency must be loaded with its Members). Site admins can do
// everything a member can.
//
// A deactivated agency doesn't exist for the public: handlers answer 404 (CanView false) before
// they ever answer 403, so outsiders can't tell it from one that never existed. A signed-in member
// whose role isn't enough gets 403.
public static class AgencyAccess
{
    public static bool CanView(Agency agency, Guid? userId, bool isAdmin) =>
        agency.Status == AgencyStatus.Active || isAdmin || (userId is { } id && agency.IsMember(id));

    // Edit the profile and logo, deactivate/reactivate.
    public static bool CanEdit(Agency agency, Guid userId, bool isAdmin) =>
        isAdmin || agency.RoleOf(userId) is AgencyRole.Owner or AgencyRole.Admin;

    // Invite, remove and change the roles of members (only Owners touch Owners and Admins).
    public static bool CanManageMembers(Agency agency, Guid userId, bool isAdmin) =>
        isAdmin || agency.RoleOf(userId) is AgencyRole.Owner or AgencyRole.Admin;

    public static bool CanDelete(Agency agency, Guid userId, bool isAdmin) =>
        isAdmin || agency.RoleOf(userId) is AgencyRole.Owner;

    // Publish a listing under it: any member, while it's active.
    public static bool CanPublishAs(Agency agency, Guid userId) =>
        agency.Status == AgencyStatus.Active && agency.IsMember(userId);

    // The caller may see the agency (else the handler returns null → 404) and edit it (else 403).
    public static bool EnsureCanEdit(Agency agency, Guid userId, bool isAdmin)
    {
        if (!CanView(agency, userId, isAdmin))
        {
            return false;
        }

        if (!CanEdit(agency, userId, isAdmin))
        {
            throw new ForbiddenAccessException();
        }

        return true;
    }
}
