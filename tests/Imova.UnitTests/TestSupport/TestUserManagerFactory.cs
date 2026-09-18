using Imova.Application.Common.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Imova.UnitTests.TestSupport;

internal static class TestUserManagerFactory
{
    // No user/password validators are wired in — that's Identity's own concern (already covered
    // by the framework, not something this codebase needs to re-test), so an empty list keeps
    // these tests focused purely on the handler's find-or-create/role/claims logic.
    public static UserManager<ApplicationUser> Create(FakeUserStore store) =>
        new(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
}
