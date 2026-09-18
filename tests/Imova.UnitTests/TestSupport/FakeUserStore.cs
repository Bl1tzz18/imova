using Imova.Application.Common.Identity;
using Microsoft.AspNetCore.Identity;

namespace Imova.UnitTests.TestSupport;

// A minimal in-memory IUserStore, used to construct a real UserManager<ApplicationUser> without
// a database — lets Auth handler tests (e.g. GoogleLoginHandler's find-or-create logic) exercise
// the actual UserManager code path instead of re-implementing its behavior in a mock.
// Only implements the store interfaces the handlers under test actually call.
internal sealed class FakeUserStore :
    IUserStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>,
    IUserRoleStore<ApplicationUser>
{
    private readonly Dictionary<Guid, ApplicationUser> _usersById = [];
    private readonly Dictionary<Guid, HashSet<string>> _rolesByUserId = [];

    public IReadOnlyCollection<ApplicationUser> Users => _usersById.Values;

    public IReadOnlyCollection<string> GetRolesSnapshot(Guid userId) =>
        _rolesByUserId.TryGetValue(userId, out var roles) ? roles.ToList() : [];

    public ApplicationUser SeedUser(string email, bool emailConfirmed)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
        };
        _usersById[user.Id] = user;
        return user;
    }

    // IUserStore<ApplicationUser>
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _usersById[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _usersById[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _usersById.Remove(user.Id);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_usersById.GetValueOrDefault(Guid.Parse(userId)));

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Task.FromResult(_usersById.Values.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName));

    public void Dispose()
    {
    }

    // IUserEmailStore<ApplicationUser>
    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(_usersById.Values.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail));

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // IUserRoleStore<ApplicationUser>
    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        if (!_rolesByUserId.TryGetValue(user.Id, out var roles))
        {
            roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _rolesByUserId[user.Id] = roles;
        }

        roles.Add(roleName);
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        if (_rolesByUserId.TryGetValue(user.Id, out var roles))
        {
            roles.Remove(roleName);
        }

        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult<IList<string>>(
            _rolesByUserId.TryGetValue(user.Id, out var roles) ? roles.ToList() : []);

    public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) =>
        Task.FromResult(_rolesByUserId.TryGetValue(user.Id, out var roles) && roles.Contains(roleName));

    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) =>
        Task.FromResult<IList<ApplicationUser>>(
            _usersById.Values.Where(u => _rolesByUserId.TryGetValue(u.Id, out var roles) && roles.Contains(roleName)).ToList());
}
