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
    public static UserManager<ApplicationUser> Create(FakeUserStore store)
    {
        var userManager = new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

        // Stands in for the data-protection provider AddDefaultTokenProviders() registers in the app.
        userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, new FakeTokenProvider());
        return userManager;
    }
}

// Reset/confirmation tokens without data protection: a token is just "<purpose>:<user id>", so
// tests can also build a valid one themselves (FakeTokenProvider.TokenFor).
internal sealed class FakeTokenProvider : IUserTwoFactorTokenProvider<ApplicationUser>
{
    public static string TokenFor(string purpose, ApplicationUser user) => $"{purpose}:{user.Id}";

    public Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user) =>
        Task.FromResult(TokenFor(purpose, user));

    public Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user) =>
        Task.FromResult(token == TokenFor(purpose, user));

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
        Task.FromResult(false);
}
