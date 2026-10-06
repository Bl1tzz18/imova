using Imova.Domain.Agencies;

namespace Imova.UnitTests.Agencies;

public class AgencyTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static AgencyProfile Profile(
        string name = "Casa Ta Imobiliare",
        string phone = "+373 22 555 010",
        string email = "office@casata.md",
        string? bio = null,
        string? website = null,
        string? address = null,
        Guid? raionId = null) =>
        new(name, phone, email, bio, website, address, raionId);

    [Fact]
    public void Create_MakesTheCreatorItsOnlyOwner_AndStartsActiveAndUnverified()
    {
        var agency = Agency.Create(Profile(), "casa-ta-imobiliare", OwnerId, Now);

        var member = Assert.Single(agency.Members);
        Assert.Equal(OwnerId, member.UserId);
        Assert.Equal(agency.Id, member.AgencyId);
        Assert.Equal(AgencyRole.Owner, member.Role);
        Assert.Equal(Now, member.JoinedAt);
        Assert.Equal(AgencyStatus.Active, agency.Status);
        Assert.False(agency.IsVerified);
        Assert.Null(agency.VerifiedAt);
        Assert.Equal(OwnerId, agency.CreatedByUserId);
        Assert.Equal(Now, agency.CreatedAt);
        Assert.Equal(Now, agency.UpdatedAt);
        Assert.Null(agency.LogoBlobName);
    }

    [Fact]
    public void Create_TrimsEverything_AndTurnsBlankOptionalFieldsIntoNull()
    {
        var raionId = Guid.NewGuid();
        var agency = Agency.Create(
            Profile(" Casa Ta ", " +373 22 555 010 ", " office@casata.md ", "  ", " https://casata.md ", "\t", raionId),
            "casa-ta",
            OwnerId,
            Now);

        Assert.Equal("Casa Ta", agency.Name);
        Assert.Equal("+373 22 555 010", agency.Phone);
        Assert.Equal("office@casata.md", agency.Email);
        Assert.Null(agency.Bio);
        Assert.Equal("https://casata.md", agency.Website);
        Assert.Null(agency.Address);
        Assert.Equal(raionId, agency.RaionId);
    }

    [Fact]
    public void Create_WithAnEmptyRaionId_HasNoCity()
    {
        Assert.Null(Agency.Create(Profile(raionId: Guid.Empty), "casa-ta", OwnerId, Now).RaionId);
    }

    [Fact]
    public void Create_KeepsTheGivenId()
    {
        var id = Guid.NewGuid();

        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now, id);

        Assert.Equal(id, agency.Id);
        Assert.Equal(id, Assert.Single(agency.Members).AgencyId);
    }

    [Theory]
    [InlineData("", "+373 22 555 010", "office@casata.md")]
    [InlineData("Casa Ta", " ", "office@casata.md")]
    [InlineData("Casa Ta", "+373 22 555 010", "")]
    public void Create_WithoutNamePhoneOrEmail_Throws(string name, string phone, string email)
    {
        Assert.Throws<ArgumentException>(() => Agency.Create(Profile(name, phone, email), "casa-ta", OwnerId, Now));
    }

    [Fact]
    public void Create_WithATooLongField_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Agency.Create(Profile(name: new string('a', Agency.MaxNameLength + 1)), "casa-ta", OwnerId, Now));
        Assert.Throws<ArgumentException>(() =>
            Agency.Create(Profile(bio: new string('a', Agency.MaxBioLength + 1)), "casa-ta", OwnerId, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutASlug_Throws(string slug)
    {
        Assert.Throws<ArgumentException>(() => Agency.Create(Profile(), slug, OwnerId, Now));
    }

    [Fact]
    public void Create_WithoutAnOwner_Throws()
    {
        Assert.Throws<ArgumentException>(() => Agency.Create(Profile(), "casa-ta", Guid.Empty, Now));
    }

    [Fact]
    public void UpdateProfile_WithANewSlug_ReturnsTheOneItReplaced()
    {
        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now);
        var later = Now.AddDays(1);

        var replaced = agency.UpdateProfile(Profile(name: "Casa Noastră", bio: "Din 2005."), "casa-noastra", later);

        Assert.Equal("casa-ta", replaced);
        Assert.Equal("casa-noastra", agency.Slug);
        Assert.Equal("Casa Noastră", agency.Name);
        Assert.Equal("Din 2005.", agency.Bio);
        Assert.Equal(later, agency.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_KeepingTheSlug_ReplacesNothing()
    {
        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now);

        Assert.Null(agency.UpdateProfile(Profile(phone: "+373 69 000 111"), "casa-ta", Now));
        Assert.Equal("+373 69 000 111", agency.Phone);
    }

    [Fact]
    public void UpdateProfile_WithAnInvalidProfile_Throws()
    {
        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now);

        Assert.Throws<ArgumentException>(() => agency.UpdateProfile(Profile(email: " "), "casa-ta", Now));
    }

    [Fact]
    public void SetLogo_ReturnsThePreviousOne_AndRemoveLogoClearsIt()
    {
        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now);

        Assert.Null(agency.SetLogo("first.jpg", Now));
        Assert.Equal("first.jpg", agency.SetLogo("second.jpg", Now));
        Assert.Equal("second.jpg", agency.LogoBlobName);

        Assert.Equal("second.jpg", agency.RemoveLogo(Now));
        Assert.Null(agency.LogoBlobName);
        Assert.Null(agency.RemoveLogo(Now));
    }

    [Fact]
    public void AddMember_AddsThemWithTheirRole_ButNeverTwice()
    {
        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now);
        var agentId = Guid.NewGuid();

        agency.AddMember(agentId, AgencyRole.Agent, Now);

        Assert.Equal(AgencyRole.Agent, agency.RoleOf(agentId));
        Assert.Throws<InvalidOperationException>(() => agency.AddMember(agentId, AgencyRole.Admin, Now));
        Assert.Throws<InvalidOperationException>(() => agency.AddMember(OwnerId, AgencyRole.Agent, Now));
    }

    [Fact]
    public void RoleOf_IsTheMembersRole_OrNullForSomeoneElse()
    {
        var agency = Agency.Create(Profile(), "casa-ta", OwnerId, Now);

        Assert.Equal(AgencyRole.Owner, agency.RoleOf(OwnerId));
        Assert.True(agency.IsMember(OwnerId));
        Assert.Null(agency.RoleOf(Guid.NewGuid()));
        Assert.False(agency.IsMember(Guid.NewGuid()));
    }
}
