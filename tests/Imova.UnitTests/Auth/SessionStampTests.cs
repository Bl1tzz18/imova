using Imova.Application.Common.Identity;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class SessionStampTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();

    private ApplicationUser AddUser(string stamp)
    {
        var user = ListingTestData.AddUser(_db, Guid.NewGuid());
        user.SecurityStamp = stamp;
        _db.SaveChanges();
        return user;
    }

    [Fact]
    public void For_IsAStableFingerprint_NotTheStampItself()
    {
        Assert.Equal(SessionStamp.For("abc"), SessionStamp.For("abc"));
        Assert.NotEqual(SessionStamp.For("abc"), SessionStamp.For("abd"));
        Assert.DoesNotContain("abc", SessionStamp.For("abc"));
        Assert.Equal(32, SessionStamp.For("abc").Length);
    }

    [Fact]
    public async Task ATokenFromTheCurrentStamp_IsCurrent()
    {
        var user = AddUser("stamp-1");

        Assert.True(await SessionStamp.IsCurrentAsync(_db, user.Id, SessionStamp.For("stamp-1"), CancellationToken.None));
    }

    [Fact]
    public async Task ATokenFromAnOlderStamp_IsNot()
    {
        var user = AddUser("stamp-2");

        Assert.False(await SessionStamp.IsCurrentAsync(_db, user.Id, SessionStamp.For("stamp-1"), CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ATokenWithoutTheClaim_IsNot(string? claim)
    {
        var user = AddUser("stamp-1");

        Assert.False(await SessionStamp.IsCurrentAsync(_db, user.Id, claim, CancellationToken.None));
    }

    [Fact]
    public async Task ATokenForAnAccountThatNoLongerExists_IsNot()
    {
        Assert.False(await SessionStamp.IsCurrentAsync(_db, Guid.NewGuid(), SessionStamp.For("stamp-1"), CancellationToken.None));
    }
}
