using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

// Who may run the CSV import (templates, guide, upload, results): site admins only — importing an
// agency's listings is a service IMOVA does by hand (an agency writes to us; see the Anunțuri tab). The
// routes are gated with RequireAdmin(); this is the second line. Preparing a file and reading past
// imports is allowed before the agency is verified; uploading one also needs it verified and active.
public static class ImportAccess
{
    // The partial unique index that keeps one waiting / running import per agency (see the
    // configuration); the upload turns a clash on it into a 409.
    public const string ActiveJobIndex = "IX_ListingImportJobs_AgencyId_Active";

    // The agency to upload for, or null (404). 403 for anyone but a site admin, and — with the reason
    // as its code — for an agency that isn't verified or is deactivated.
    public static async Task<Domain.Agencies.Agency?> EnsureCanUploadAsync(
        IApplicationDbContext dbContext, Guid agencyId, bool isAdmin, CancellationToken cancellationToken)
    {
        EnsureAdmin(isAdmin);
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == agencyId, cancellationToken);
        if (agency is null)
        {
            return null;
        }

        if (!agency.IsVerified)
        {
            throw new ForbiddenAccessException(
                "Only a verified agency can import listings.", Common.ErrorCodes.ImportAgencyNotVerified);
        }

        if (agency.Status != Domain.Agencies.AgencyStatus.Active)
        {
            throw new ForbiddenAccessException(
                "A deactivated agency can't import listings.", Common.ErrorCodes.ImportAgencyInactive);
        }

        return agency;
    }

    // False → 404 (no such agency); anyone but a site admin → 403.
    public static async Task<bool> CanPrepareAsync(
        IApplicationDbContext dbContext, Guid agencyId, bool isAdmin, CancellationToken cancellationToken)
    {
        EnsureAdmin(isAdmin);
        return await dbContext.Agencies.AsNoTracking().AnyAsync(a => a.Id == agencyId, cancellationToken);
    }

    private static void EnsureAdmin(bool isAdmin)
    {
        if (!isAdmin)
        {
            throw new ForbiddenAccessException();
        }
    }
}
