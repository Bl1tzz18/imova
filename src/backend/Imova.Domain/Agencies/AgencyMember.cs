namespace Imova.Domain.Agencies;

// A user's membership of an agency. A user can belong to several agencies, once each (the key is
// AgencyId + UserId).
public sealed class AgencyMember
{
    private AgencyMember(Guid agencyId, Guid userId, AgencyRole role, DateTimeOffset joinedAt)
    {
        AgencyId = agencyId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public Guid AgencyId { get; private set; }

    public Guid UserId { get; private set; }

    public AgencyRole Role { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    internal static AgencyMember Create(Guid agencyId, Guid userId, AgencyRole role, DateTimeOffset joinedAt) =>
        new(agencyId, userId, role, joinedAt);

    internal void SetRole(AgencyRole role) => Role = role;
}
