using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Publishers;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Members;

// The agency keeps its listings when one of its people leaves: they're handed to an Owner or Admin
// who stays (the heir). Conversations already started stay with whoever they're with; new ones go
// to the new author.
public static class AgencyListingReassignment
{
    // The heir when none was chosen: whoever is removing them (if an Owner/Admin who stays), else
    // the longest-standing Owner, else the longest-standing Admin. Null when nobody qualifies.
    public static Guid? DefaultHeir(Agency agency, Guid leavingUserId, Guid? actorId)
    {
        if (actorId is { } actor && actor != leavingUserId && agency.RoleOf(actor) is AgencyRole.Owner or AgencyRole.Admin)
        {
            return actor;
        }

        return agency.Members
            .Where(m => m.UserId != leavingUserId && m.Role is AgencyRole.Owner or AgencyRole.Admin)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m => (Guid?)m.UserId)
            .FirstOrDefault();
    }

    // Whether `heirId` may take the listings over: an Owner or Admin of the agency who isn't the one leaving.
    public static bool IsValidHeir(Agency agency, Guid leavingUserId, Guid heirId) =>
        heirId != leavingUserId && agency.RoleOf(heirId) is AgencyRole.Owner or AgencyRole.Admin;

    // How many listings `fromUserId` authored under the agency.
    public static Task<int> CountAsync(IApplicationDbContext dbContext, Guid agencyId, Guid fromUserId, CancellationToken cancellationToken) =>
        dbContext.Listings.CountAsync(
            l => l.AgencyId == agencyId && dbContext.Publishers.Any(p => p.Id == l.PublisherId && p.UserId == fromUserId),
            cancellationToken);

    // Moves the listings (without saving). `heirId` null with listings to move → 400 agency.reassignInvalid.
    public static async Task MoveAsync(
        IApplicationDbContext dbContext, Agency agency, Guid fromUserId, Guid? heirId, CancellationToken cancellationToken)
    {
        var listings = await dbContext.Listings
            .Where(l => l.AgencyId == agency.Id && dbContext.Publishers.Any(p => p.Id == l.PublisherId && p.UserId == fromUserId))
            .ToListAsync(cancellationToken);
        if (listings.Count == 0)
        {
            return;
        }

        if (heirId is not { } heir || !IsValidHeir(agency, fromUserId, heir))
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    "ReassignTo",
                    "Their listings must go to an owner or administrator who stays in the agency.",
                    ErrorCodes.AgencyReassignInvalid),
            ]);
        }

        var heirPublisher = await PublisherProvisioning.EnsureIndividualAsync(dbContext, heir, cancellationToken);
        foreach (var listing in listings)
        {
            listing.ChangeAuthor(heirPublisher.Id);
        }
    }
}
