using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Agencies.Logos;
using Imova.Contracts.Agencies;
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

        var listings = await dbContext.Listings.Where(l => l.AgencyId == agency.Id).ToListAsync(cancellationToken);
        foreach (var listing in listings)
        {
            listing.ChangeAgency(null);
        }

        dbContext.AgencyInvitations.RemoveRange(
            await dbContext.AgencyInvitations.Where(i => i.AgencyId == agency.Id).ToListAsync(cancellationToken));
        dbContext.AgencyFormerSlugs.RemoveRange(
            await dbContext.AgencyFormerSlugs.Where(s => s.AgencyId == agency.Id).ToListAsync(cancellationToken));
        dbContext.Agencies.Remove(agency);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Agency {AgencyId} deleted by user {UserId}; {ListingCount} listings became private.",
            agency.Id,
            request.UserId,
            listings.Count);

        // After the save, best effort: a leftover file is harmless, a missing row isn't.
        foreach (var blobName in agency.LogoBlobName is { } logo ? AgencyLogo.AllBlobNames(logo) : [])
        {
            try
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Couldn't delete the logo file {BlobName} of deleted agency {AgencyId}.", blobName, agency.Id);
            }
        }

        return true;
    }
}
