using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

// Who may import an agency's listings: its Owners and Admins, and site admins — the same people who
// edit the agency (AgencyAccess.CanEdit). Preparing a file (template, columns) is allowed before the
// agency is verified; uploading one (step 3) also needs it verified and active.
public static class ImportAccess
{
    // False → 404 (the agency doesn't exist or is hidden from the caller); a member whose role isn't
    // enough → 403 (ForbiddenAccessException, from AgencyAccess.EnsureCanEdit).
    public static async Task<bool> CanPrepareAsync(
        IApplicationDbContext dbContext, Guid agencyId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == agencyId, cancellationToken);

        return agency is not null && AgencyAccess.EnsureCanEdit(agency, userId, isAdmin);
    }
}
