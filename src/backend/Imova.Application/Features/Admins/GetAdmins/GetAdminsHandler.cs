using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Admins;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Admins.GetAdmins;

// Every admin, with who made them one and when (from the audit trail), oldest grant first.
public class GetAdminsHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext dbContext)
    : IRequestHandler<GetAdminsQuery, IReadOnlyList<AdminUserDto>>
{
    public async Task<IReadOnlyList<AdminUserDto>> Handle(GetAdminsQuery request, CancellationToken cancellationToken)
    {
        await AdminAccess.EnsureCurrentAdminAsync(userManager, request.ActorUserId);

        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        var ids = admins.Select(a => a.Id).ToList();
        var grants = (await dbContext.AdminAuditEntries.AsNoTracking()
                .Where(e => e.Action == AdminAuditEntry.GrantAdmin && ids.Contains(e.TargetUserId))
                .ToListAsync(cancellationToken))
            .GroupBy(e => e.TargetUserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First());
        var actorIds = grants.Values.Select(g => g.ActorUserId).Distinct().ToList();
        var actorEmails = await dbContext.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return admins
            .Select(a =>
            {
                var grant = grants.GetValueOrDefault(a.Id);
                return new AdminUserDto(
                    a.Id,
                    a.Email!,
                    a.DisplayName,
                    grant?.CreatedAt,
                    grant is null ? null : actorEmails.GetValueOrDefault(grant.ActorUserId));
            })
            .OrderBy(a => a.GrantedAt ?? DateTimeOffset.MinValue)
            .ThenBy(a => a.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
