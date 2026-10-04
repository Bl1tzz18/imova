using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// A listing is published under an agency only by one of its members, and only while the agency is
// active (AgencyAccess.CanPublishAs): an unknown agency → 400, not a member → 403, deactivated → 400.
public static class ListingAgencyRules
{
    public static async Task EnsureCanPublishAsAsync(
        IApplicationDbContext dbContext, Guid agencyId, Guid authorUserId, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking()
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == agencyId, cancellationToken)
            ?? throw new ValidationException(
                [CodedFailure.Of("AgencyId", "AgencyId does not reference a known agency.", ErrorCodes.ListingAgencyUnknown)]);

        if (!agency.IsMember(authorUserId))
        {
            throw new ForbiddenAccessException("Only the agency's members can publish under it.", ErrorCodes.ListingNotAgencyMember);
        }

        if (agency.Status != AgencyStatus.Active)
        {
            throw new ValidationException(
                [CodedFailure.Of("AgencyId", "This agency is deactivated; nothing can be published under it.", ErrorCodes.ListingAgencyInactive)]);
        }
    }
}
