using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Admins;

public static class AdminAccess
{
    // The caller must be an admin *now*, per the database — not merely per a login token that may
    // be up to 15 minutes old (a revoked admin keeps the role claim until the token expires).
    public static async Task<ApplicationUser> EnsureCurrentAdminAsync(UserManager<ApplicationUser> userManager, Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            throw new ForbiddenAccessException("Only an administrator can do this.", ErrorCodes.Forbidden);
        }

        return user;
    }
}
