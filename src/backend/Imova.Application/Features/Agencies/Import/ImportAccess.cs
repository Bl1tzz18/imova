using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

// Who may import an agency's listings: its Owners and Admins, and site admins (also when they aren't
// members — onboarding an agency) — the same people who edit the agency (AgencyAccess.CanEdit).
// Preparing a file (template, columns) and reading past imports is allowed before the agency is
// verified; uploading one also needs it verified and active.
public static class ImportAccess
{
    // The partial unique index that keeps one waiting / running import per agency (see the
    // configuration); the upload turns a clash on it into a 409.
    public const string ActiveJobIndex = "IX_ListingImportJobs_AgencyId_Active";

    // The agency to upload for, or null (404). 403 for a member who can't manage it, and — with the
    // reason as its code — for an agency that isn't verified or is deactivated.
    public static async Task<Domain.Agencies.Agency?> EnsureCanUploadAsync(
        IApplicationDbContext dbContext, Guid agencyId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking().Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == agencyId, cancellationToken);
        if (agency is null || !AgencyAccess.EnsureCanEdit(agency, userId, isAdmin))
        {
            return null;
        }

        if (!agency.IsVerified)
        {
            throw new Common.Exceptions.ForbiddenAccessException(
                "Only a verified agency can import listings.", Common.ErrorCodes.ImportAgencyNotVerified);
        }

        if (agency.Status != Domain.Agencies.AgencyStatus.Active)
        {
            throw new Common.Exceptions.ForbiddenAccessException(
                "A deactivated agency can't import listings.", Common.ErrorCodes.ImportAgencyInactive);
        }

        return agency;
    }
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
