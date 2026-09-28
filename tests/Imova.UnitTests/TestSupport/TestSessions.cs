using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Auth.Sessions;
using Microsoft.AspNetCore.Identity;

namespace Imova.UnitTests.TestSupport;

// AuthSessions for handler tests: refresh tokens in an in-memory database, fake login tokens.
internal static class TestSessions
{
    public static AuthSessions For(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext? dbContext = null,
        TimeProvider? timeProvider = null,
        FakeJwtTokenGenerator? jwt = null,
        AuthSessionOptions? options = null) =>
        new(
            dbContext ?? TestDbContextFactory.Create(),
            userManager,
            jwt ?? new FakeJwtTokenGenerator(),
            options ?? new AuthSessionOptions(),
            timeProvider ?? TimeProvider.System);
}
