using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Admins;
using Imova.Application.Features.Admins.GetAdmins;
using Imova.Application.Features.Admins.GrantAdmin;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Admins;

// Making someone an admin: only a current admin, only with their own password, only for an existing
// confirmed account — audited, and announced to the new admin and every admin.
public class GrantAdminHandlerTests
{
    private const string Password = "Secret123!";

    private readonly FakeUserStore _store = new();
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeEmailSender _email = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly UserManager<ApplicationUser> _userManager;

    public GrantAdminHandlerTests()
    {
        _userManager = TestUserManagerFactory.Create(_store);
    }

    // In the Identity store (password, roles) and in the database (emails for the admins list).
    private async Task<ApplicationUser> SeedAsync(string email, bool admin = false, bool withPassword = true, bool confirmed = true)
    {
        var user = withPassword ? _store.SeedUserWithPassword(email, Password) : _store.SeedUser(email, confirmed);
        user.EmailConfirmed = confirmed;
        if (admin)
        {
            await _store.AddToRoleAsync(user, Roles.Admin, CancellationToken.None);
        }

        _db.Users.Add(new ApplicationUser { Id = user.Id, Email = email, UserName = email });
        await _db.SaveChangesAsync();
        return user;
    }

    private Task GrantAsync(Guid actorId, string email, string? password = Password) =>
        new GrantAdminHandler(_userManager, _db, new AdminEmails(_email, new AppOptions()), _clock, NullLogger<GrantAdminHandler>.Instance)
            .Handle(new GrantAdminCommand(actorId, email, password, "203.0.113.7"), CancellationToken.None);

    private Task<bool> IsAdminAsync(ApplicationUser user) => _userManager.IsInRoleAsync(user, Roles.Admin);

    private static string Code(ValidationException e) => Assert.Single(e.Errors).ErrorCode;

    [Fact]
    public async Task Grant_MakesTheAccountAnAdmin_RecordsIt_AndTellsEveryAdmin()
    {
        var actor = await SeedAsync("boss@imova.md", admin: true);
        var other = await SeedAsync("other-admin@imova.md", admin: true);
        var target = await SeedAsync("ana@example.com");

        await GrantAsync(actor.Id, "  ANA@example.com ");

        Assert.True(await IsAdminAsync(target));
        var entry = Assert.Single(_db.AdminAuditEntries);
        Assert.Equal((AdminAuditEntry.GrantAdmin, actor.Id, target.Id, _clock.Now, "203.0.113.7"),
            (entry.Action, entry.ActorUserId, entry.TargetUserId, entry.CreatedAt, entry.IpAddress));
        Assert.Contains(_email.Sent, m => m.To == "ana@example.com" && m.Subject.Contains("administrator"));
        Assert.Contains(_email.Sent, m => m.To == "boss@imova.md" && m.TextBody.Contains("ana@example.com"));
        Assert.Contains(_email.Sent, m => m.To == other.Email && m.TextBody.Contains("boss@imova.md"));
        Assert.Equal(3, _email.Sent.Count);
    }

    [Fact]
    public async Task Grant_BySomeoneWhoIsNotAnAdminInTheDatabase_IsForbidden()
    {
        // A stale token may still carry the role — the database is what counts.
        var formerAdmin = await SeedAsync("former@imova.md");
        var target = await SeedAsync("ana@example.com");

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => GrantAsync(formerAdmin.Id, "ana@example.com"));

        Assert.False(await IsAdminAsync(target));
        Assert.Empty(_db.AdminAuditEntries);
    }

    [Fact]
    public async Task Grant_WithoutThePassword_IsRefused()
    {
        var actor = await SeedAsync("boss@imova.md", admin: true);
        var target = await SeedAsync("ana@example.com");

        var error = await Assert.ThrowsAsync<ValidationException>(() => GrantAsync(actor.Id, "ana@example.com", password: null));

        Assert.Equal(ErrorCodes.AccountPasswordRequired, Code(error));
        Assert.False(await IsAdminAsync(target));
    }

    [Fact]
    public async Task Grant_WithAWrongPassword_IsRefused_AndLocksOutAfterFiveTries()
    {
        var actor = await SeedAsync("boss@imova.md", admin: true);
        var target = await SeedAsync("ana@example.com");

        for (var i = 0; i < 4; i++)
        {
            var error = await Assert.ThrowsAsync<ValidationException>(() => GrantAsync(actor.Id, "ana@example.com", "Wrong123!"));
            Assert.Equal(ErrorCodes.AccountWrongPassword, Code(error));
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() => GrantAsync(actor.Id, "ana@example.com", "Wrong123!"));
        // Locked out: even the right password is refused now.
        await Assert.ThrowsAsync<TooManyRequestsException>(() => GrantAsync(actor.Id, "ana@example.com"));
        Assert.False(await IsAdminAsync(target));
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Grant_ByAnAdminWithoutAPassword_AsksThemToSetOneFirst()
    {
        var actor = await SeedAsync("google-admin@imova.md", admin: true, withPassword: false);
        await SeedAsync("ana@example.com");

        var error = await Assert.ThrowsAsync<ValidationException>(() => GrantAsync(actor.Id, "ana@example.com", "anything"));

        Assert.Equal(ErrorCodes.AdminPasswordNeeded, Code(error));
    }

    [Fact]
    public async Task Grant_ToAnUnknownEmail_AnUnconfirmedAccountOrAnAdmin_IsRefused()
    {
        var actor = await SeedAsync("boss@imova.md", admin: true);
        var unconfirmed = await SeedAsync("new@example.com", confirmed: false);
        await SeedAsync("admin2@imova.md", admin: true);

        Assert.Equal(ErrorCodes.AdminUserNotFound,
            Code(await Assert.ThrowsAsync<ValidationException>(() => GrantAsync(actor.Id, "nobody@example.com"))));
        Assert.Equal(ErrorCodes.AdminEmailNotConfirmed,
            Code(await Assert.ThrowsAsync<ValidationException>(() => GrantAsync(actor.Id, "new@example.com"))));
        Assert.Equal(ErrorCodes.AlreadyAdmin,
            Code(await Assert.ThrowsAsync<ValidationException>(() => GrantAsync(actor.Id, "admin2@imova.md"))));

        Assert.False(await IsAdminAsync(unconfirmed));
        Assert.Empty(_db.AdminAuditEntries);
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Grant_StillSucceeds_WhenTheEmailsCannotBeSent()
    {
        var actor = await SeedAsync("boss@imova.md", admin: true);
        var target = await SeedAsync("ana@example.com");
        _email.Fail = true;

        await GrantAsync(actor.Id, "ana@example.com");

        Assert.True(await IsAdminAsync(target));
    }

    [Fact]
    public async Task AdminsList_ShowsWhoGrantedWhomAndWhen_AndIsForAdminsOnly()
    {
        var actor = await SeedAsync("boss@imova.md", admin: true);
        var target = await SeedAsync("ana@example.com");
        var stranger = await SeedAsync("stranger@example.com");
        await GrantAsync(actor.Id, "ana@example.com");
        var handler = new GetAdminsHandler(_userManager, _db);

        var admins = await handler.Handle(new GetAdminsQuery(actor.Id), CancellationToken.None);

        Assert.Equal(["boss@imova.md", "ana@example.com"], admins.Select(a => a.Email));
        Assert.Null(admins[0].GrantedAt);
        Assert.Equal((target.Id, _clock.Now, "boss@imova.md"), (admins[1].Id, admins[1].GrantedAt, admins[1].GrantedBy));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(new GetAdminsQuery(stranger.Id), CancellationToken.None));
    }

    [Fact]
    public void Validator_NeedsAValidEmail()
    {
        var validator = new GrantAdminValidator();

        Assert.False(validator.Validate(new GrantAdminCommand(Guid.NewGuid(), "not-an-email", Password, null)).IsValid);
        Assert.False(validator.Validate(new GrantAdminCommand(Guid.NewGuid(), "", Password, null)).IsValid);
        Assert.True(validator.Validate(new GrantAdminCommand(Guid.NewGuid(), "ana@example.com", Password, null)).IsValid);
    }
}
