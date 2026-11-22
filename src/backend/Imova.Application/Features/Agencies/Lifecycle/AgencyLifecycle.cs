using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Agencies.Logos;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Lifecycle;

// Deactivating hides the agency and every one of its listings from the public (ListingVisibility);
// its members still see and manage everything, and reactivate it the same way. Owners, Admins and
// site admins. Null (404) when the caller may not see the agency; 403 for a member whose role isn't
// enough.
public record SetAgencyActiveCommand(Guid AgencyId, Guid UserId, bool IsAdmin, bool Active) : IRequest<AgencyDto?>;

public class SetAgencyActiveHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService, TimeProvider timeProvider)
    : IRequestHandler<SetAgencyActiveCommand, AgencyDto?>
{
    public async Task<AgencyDto?> Handle(SetAgencyActiveCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.EnsureCanEdit(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (request.Active)
        {
            agency.Reactivate(now);
        }
        else
        {
            agency.Deactivate(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, request.IsAdmin, cancellationToken);
    }
}

// Deletes the agency for good — an Owner or a site admin, typing its exact name to confirm
// (agency.confirmNameMismatch otherwise). Its listings stay, as private listings of their authors
// (AgencyId and the agency's ExternalRef cleared); its members, invitations, former slugs and logo go.
// Conversations about its listings stay where they are. False (404) when the caller may not see it.
public record DeleteAgencyCommand(Guid AgencyId, Guid UserId, bool IsAdmin, string? ConfirmName) : IRequest<bool>;

public class DeleteAgencyHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ILogger<DeleteAgencyHandler> logger) : IRequestHandler<DeleteAgencyCommand, bool>
{
    public async Task<bool> Handle(DeleteAgencyCommand request, CancellationToken cancellationToken)
    {
        var agency = await AgencyDtoLoader.FindAsync(dbContext, request.AgencyId, cancellationToken);
        if (agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin))
        {
            return false;
        }

        if (!AgencyAccess.CanDelete(agency, request.UserId, request.IsAdmin))
        {
            throw new ForbiddenAccessException();
        }

        if (!string.Equals(request.ConfirmName?.Trim(), agency.Name, StringComparison.Ordinal))
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    nameof(DeleteAgencyCommand.ConfirmName),
                    "Type the agency's exact name to delete it.",
                    ErrorCodes.AgencyConfirmNameMismatch),
            ]);
        }

        var removal = await AgencyRemoval.StageAsync(dbContext, agency, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Agency {AgencyId} deleted by user {UserId}; {ListingCount} listings became private.",
            agency.Id,
            request.UserId,
            removal.ListingCount);

        await AgencyRemoval.DeleteFilesAsync(blobStorageService, removal, logger, cancellationToken);
        return true;
    }
}

// What deleting an agency does, shared by an Owner's delete (DeleteAgencyHandler) and a site admin's
// (AdminDeleteAgencyHandler) so the two can't drift apart. StageAsync marks everything for removal
// in the caller's unit of work: its listings stay, as private listings of their authors (AgencyId and
// the agency's ExternalRef cleared) — favorites, conversations, views and price history untouched;
// its invitations, former slugs and the agency go, and with it (cascade) its members, phone-reveal
// marks and CSV import jobs + rows. User accounts are never touched. After the caller's save,
// DeleteFilesAsync removes the logo and the kept import files, best effort.
public static class AgencyRemoval
{
    public sealed record Staged(Guid AgencyId, int ListingCount, IReadOnlyList<string> LogoBlobNames, IReadOnlyList<string> ImportFiles);

    public static async Task<Staged> StageAsync(IApplicationDbContext dbContext, Agency agency, CancellationToken cancellationToken)
    {
        var listings = await dbContext.Listings.Where(l => l.AgencyId == agency.Id).ToListAsync(cancellationToken);
        foreach (var listing in listings)
        {
            listing.ChangeAgency(null);
        }

        // Its CSV imports go with it (rows cascade); their kept files are deleted after the save.
        var importFiles = await dbContext.ListingImportJobs
            .Where(j => j.AgencyId == agency.Id && j.FileBlobName != null)
            .Select(j => j.FileBlobName!)
            .ToListAsync(cancellationToken);

        dbContext.AgencyInvitations.RemoveRange(
            await dbContext.AgencyInvitations.Where(i => i.AgencyId == agency.Id).ToListAsync(cancellationToken));
        dbContext.AgencyFormerSlugs.RemoveRange(
            await dbContext.AgencyFormerSlugs.Where(s => s.AgencyId == agency.Id).ToListAsync(cancellationToken));
        dbContext.Agencies.Remove(agency);

        return new Staged(
            agency.Id,
            listings.Count,
            agency.LogoBlobName is { } logo ? AgencyLogo.AllBlobNames(logo).ToList() : [],
            importFiles);
    }

    // After the save: a leftover file is harmless, a missing row isn't.
    public static async Task DeleteFilesAsync(
        IBlobStorageService blobStorageService, Staged removal, ILogger logger, CancellationToken cancellationToken)
    {
        foreach (var blobName in removal.LogoBlobNames)
        {
            try
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Couldn't delete the logo file {BlobName} of deleted agency {AgencyId}.", blobName, removal.AgencyId);
            }
        }

        foreach (var blobName in removal.ImportFiles)
        {
            try
            {
                await blobStorageService.DeletePrivateAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Couldn't delete the import file {BlobName} of deleted agency {AgencyId}.", blobName, removal.AgencyId);
            }
        }
    }
}
