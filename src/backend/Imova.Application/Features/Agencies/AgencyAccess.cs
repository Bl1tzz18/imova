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

    // Owners can do anything to anyone; Admins only to Agents (only Owners add, remove, promote or
    // demote Owners and Admins). Anyone may step down themselves. The last-Owner rule is the
    // agency's own (Agency.ChangeRole).
    public static bool CanChangeRole(Agency agency, Guid actorId, bool isAdmin, Guid targetId, AgencyRole newRole)
    {
        if (isAdmin || agency.RoleOf(actorId) is AgencyRole.Owner)
        {
            return true;
        }

        var actorRole = agency.RoleOf(actorId);
        var targetRole = agency.RoleOf(targetId);
        if (actorId == targetId && actorRole is { } own && newRole >= own)
        {
            // Stepping down (roles are ordered Owner < Admin < Agent).
            return true;
        }

        return actorRole is AgencyRole.Admin && targetRole is AgencyRole.Agent && newRole is AgencyRole.Agent;
    }

    // Leaving is always allowed (bar the last Owner); removing someone else follows CanChangeRole's rule.
    public static bool CanRemoveMember(Agency agency, Guid actorId, bool isAdmin, Guid targetId) =>
        actorId == targetId
        || isAdmin
        || agency.RoleOf(actorId) is AgencyRole.Owner
        || (agency.RoleOf(actorId) is AgencyRole.Admin && agency.RoleOf(targetId) is AgencyRole.Agent);

    // Admins may invite Agents; only Owners invite Admins.
    public static bool CanInvite(Agency agency, Guid actorId, bool isAdmin, AgencyRole role) =>
        isAdmin
        || agency.RoleOf(actorId) is AgencyRole.Owner
        || (agency.RoleOf(actorId) is AgencyRole.Admin && role is AgencyRole.Agent);

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
