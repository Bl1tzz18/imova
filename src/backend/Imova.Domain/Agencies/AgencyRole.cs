namespace Imova.Domain.Agencies;

// What a member may do in an agency. Owners and Admins manage the agency and every one of its
// listings; Agents manage only the listings they published. Only Owners can add, remove or demote
// Owners and Admins, and an agency always keeps at least one Owner.
public enum AgencyRole
{
    Owner = 1,
    Admin = 2,
    Agent = 3,
}
