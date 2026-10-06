using Imova.Application.Features.Agencies;
using Imova.Domain.Agencies;

namespace Imova.UnitTests.Agencies;

public class AgencyMembershipTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Owner = Guid.NewGuid();

    private static Agency NewAgency(params (Guid UserId, AgencyRole Role)[] others)
    {
        var agency = Agency.Create(new AgencyProfile("Casa Ta", "+373 22 555 010", "o@casata.md"), "casa-ta", Owner, Now);
        foreach (var (userId, role) in others)
        {
            agency.AddMember(userId, role, Now);
        }

        return agency;
    }

    // --- The last-Owner rule ---

    [Fact]
    public void TheLastOwner_CanNeitherStepDownNorLeave()
    {
        var agency = NewAgency((Guid.NewGuid(), AgencyRole.Agent));

        Assert.Throws<LastOwnerException>(() => agency.ChangeRole(Owner, AgencyRole.Admin, Now));
        Assert.Throws<LastOwnerException>(() => agency.RemoveMember(Owner, Now));
        Assert.Equal(AgencyRole.Owner, agency.RoleOf(Owner));
    }

    [Fact]
    public void Ownership_IsHandedOverByPromotingSomeoneThenSteppingDown()
    {
        var next = Guid.NewGuid();
        var agency = NewAgency((next, AgencyRole.Admin));

        agency.ChangeRole(next, AgencyRole.Owner, Now);
        agency.ChangeRole(Owner, AgencyRole.Agent, Now);

        Assert.Equal(AgencyRole.Owner, agency.RoleOf(next));
        Assert.Equal(AgencyRole.Agent, agency.RoleOf(Owner));
        Assert.Equal(1, agency.OwnerCount);
    }

    [Fact]
    public void AnyoneButTheLastOwner_CanLeave()
    {
        var agent = Guid.NewGuid();
        var agency = NewAgency((agent, AgencyRole.Agent));

        agency.RemoveMember(agent, Now);

        Assert.False(agency.IsMember(agent));
    }

    [Fact]
    public void ChangingOrRemovingANonMember_Throws()
    {
        var agency = NewAgency();

        Assert.Throws<InvalidOperationException>(() => agency.ChangeRole(Guid.NewGuid(), AgencyRole.Agent, Now));
        Assert.Throws<InvalidOperationException>(() => agency.RemoveMember(Guid.NewGuid(), Now));
    }

    // --- Who may do what ---

    [Theory]
    // actor, target's role, new role, allowed
    [InlineData(AgencyRole.Owner, AgencyRole.Agent, AgencyRole.Admin, true)]
    [InlineData(AgencyRole.Owner, AgencyRole.Admin, AgencyRole.Owner, true)]
    [InlineData(AgencyRole.Owner, AgencyRole.Admin, AgencyRole.Agent, true)]
    [InlineData(AgencyRole.Admin, AgencyRole.Agent, AgencyRole.Admin, false)]
    [InlineData(AgencyRole.Admin, AgencyRole.Admin, AgencyRole.Agent, false)]
    [InlineData(AgencyRole.Admin, AgencyRole.Agent, AgencyRole.Owner, false)]
    [InlineData(AgencyRole.Agent, AgencyRole.Agent, AgencyRole.Admin, false)]
    public void ChangingSomeoneElsesRole_OnlyOwnersTouchOwnersAndAdmins(AgencyRole actorRole, AgencyRole targetRole, AgencyRole newRole, bool allowed)
    {
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        var agency = NewAgency((actor, actorRole), (target, targetRole));

        Assert.Equal(allowed, AgencyAccess.CanChangeRole(agency, actor, false, target, newRole));
    }

    [Theory]
    [InlineData(AgencyRole.Admin, AgencyRole.Agent, true)]
    [InlineData(AgencyRole.Admin, AgencyRole.Owner, false)]
    [InlineData(AgencyRole.Agent, AgencyRole.Admin, false)]
    public void SteppingDownYourself_IsAllowed_SteppingUpIsNot(AgencyRole own, AgencyRole newRole, bool allowed)
    {
        var me = Guid.NewGuid();
        var agency = NewAgency((me, own));

        Assert.Equal(allowed, AgencyAccess.CanChangeRole(agency, me, false, me, newRole));
    }

    [Fact]
    public void ASiteAdmin_MayDoAnything()
    {
        var agency = NewAgency();
        var siteAdmin = Guid.NewGuid();

        Assert.True(AgencyAccess.CanChangeRole(agency, siteAdmin, true, Owner, AgencyRole.Agent));
        Assert.True(AgencyAccess.CanRemoveMember(agency, siteAdmin, true, Owner));
        Assert.True(AgencyAccess.CanInvite(agency, siteAdmin, true, AgencyRole.Admin));
    }

    [Fact]
    public void Removing_AdminsRemoveAgentsOnly_OwnersAnyone_EveryoneThemselves()
    {
        var admin = Guid.NewGuid();
        var otherAdmin = Guid.NewGuid();
        var agent = Guid.NewGuid();
        var agency = NewAgency((admin, AgencyRole.Admin), (otherAdmin, AgencyRole.Admin), (agent, AgencyRole.Agent));

        Assert.True(AgencyAccess.CanRemoveMember(agency, admin, false, agent));
        Assert.False(AgencyAccess.CanRemoveMember(agency, admin, false, otherAdmin));
        Assert.True(AgencyAccess.CanRemoveMember(agency, Owner, false, otherAdmin));
        Assert.True(AgencyAccess.CanRemoveMember(agency, agent, false, agent));
        Assert.False(AgencyAccess.CanRemoveMember(agency, agent, false, admin));
    }

    [Fact]
    public void Inviting_AdminsInviteAgents_OnlyOwnersInviteAdmins_AgentsNobody()
    {
        var admin = Guid.NewGuid();
        var agent = Guid.NewGuid();
        var agency = NewAgency((admin, AgencyRole.Admin), (agent, AgencyRole.Agent));

        Assert.True(AgencyAccess.CanInvite(agency, Owner, false, AgencyRole.Admin));
        Assert.True(AgencyAccess.CanInvite(agency, admin, false, AgencyRole.Agent));
        Assert.False(AgencyAccess.CanInvite(agency, admin, false, AgencyRole.Admin));
        Assert.False(AgencyAccess.CanInvite(agency, agent, false, AgencyRole.Agent));
    }

    // --- Invitations ---

    private static AgencyInvitation Invite(AgencyRole role = AgencyRole.Agent) =>
        AgencyInvitation.Create(Guid.NewGuid(), "  Ana@Example.COM ", role, "hash-1", Owner, Now);

    [Fact]
    public void AnInvitation_IsPendingForSevenDays_ForALowerCasedAddress()
    {
        var invitation = Invite();

        Assert.Equal("ana@example.com", invitation.Email);
        Assert.Equal(AgencyInvitationStatus.Pending, invitation.StatusAt(Now.AddDays(7).AddSeconds(-1)));
        Assert.Equal(AgencyInvitationStatus.Expired, invitation.StatusAt(Now.AddDays(7)));
    }

    [Fact]
    public void AnInvitation_IsNeverForAnOwner()
    {
        Assert.Throws<ArgumentException>(() => Invite(AgencyRole.Owner));
    }

    [Fact]
    public void Resending_GivesANewLinkAndSevenMoreDays_EvenAfterItExpired()
    {
        var invitation = Invite();
        var later = Now.AddDays(10);

        invitation.Resend("hash-2", AgencyRole.Admin, later);

        Assert.Equal("hash-2", invitation.TokenHash);
        Assert.Equal(AgencyRole.Admin, invitation.Role);
        Assert.Equal(later, invitation.LastSentAt);
        Assert.Equal(AgencyInvitationStatus.Pending, invitation.StatusAt(later.AddDays(6)));
    }

    [Fact]
    public void Accepting_DecliningAndRevoking_EachEndIt()
    {
        var accepted = Invite();
        var declined = Invite();
        var revoked = Invite();
        var userId = Guid.NewGuid();

        accepted.Accept(userId, Now);
        declined.Decline(Now);
        revoked.Revoke(Now);

        Assert.Equal(AgencyInvitationStatus.Accepted, accepted.StatusAt(Now));
        Assert.Equal(userId, accepted.AcceptedByUserId);
        Assert.Equal(AgencyInvitationStatus.Declined, declined.StatusAt(Now));
        Assert.Equal(AgencyInvitationStatus.Revoked, revoked.StatusAt(Now));
        Assert.Throws<InvalidOperationException>(() => accepted.Decline(Now));
        Assert.Throws<InvalidOperationException>(() => revoked.Resend("x", AgencyRole.Agent, Now));
        Assert.Throws<InvalidOperationException>(() => declined.Accept(userId, Now));
    }

    [Fact]
    public void AnExpiredInvitation_CantBeAccepted()
    {
        var invitation = Invite();

        Assert.Throws<InvalidOperationException>(() => invitation.Accept(Guid.NewGuid(), Now.AddDays(8)));
    }
}
