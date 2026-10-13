using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Account.EmailPreferences;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Account;

public class PreferredLanguageTests
{
    [Theory]
    [InlineData("ro", true)]
    [InlineData("en", true)]
    [InlineData("ru", true)]
    [InlineData("RU", false)]
    [InlineData("de", false)]
    [InlineData("", false)]
    public void Validator_AcceptsOnlyTheSitesLanguages(string language, bool valid)
    {
        var result = new UpdatePreferredLanguageValidator().Validate(new UpdatePreferredLanguageCommand(Guid.NewGuid(), language));

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public async Task Handle_SavesTheLanguageOnTheAccount_AndLeavesTheSecurityStampAlone()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var user = ListingTestData.AddUser(db, userId);
        user.SecurityStamp = "stamp-1";
        await db.SaveChangesAsync();
        Assert.Equal("ro", user.PreferredLanguage);

        await new UpdatePreferredLanguageHandler(db).Handle(new UpdatePreferredLanguageCommand(userId, "ru"), CancellationToken.None);

        var saved = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        Assert.Equal("ru", saved.PreferredLanguage);
        Assert.Equal("stamp-1", saved.SecurityStamp);
    }

    [Fact]
    public async Task Handle_ForAnAccountThatIsGone_IsUnauthenticated()
    {
        await using var db = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            new UpdatePreferredLanguageHandler(db).Handle(new UpdatePreferredLanguageCommand(Guid.NewGuid(), "en"), CancellationToken.None));
    }
}
