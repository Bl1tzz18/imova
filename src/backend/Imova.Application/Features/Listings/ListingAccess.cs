using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Who may manage a listing (edit it, change its status, its photos, see its statistics): its author
// — the account behind its Publisher — and, for a listing published under an agency, that agency's
// Owners and Admins. An Agent manages only the listings they wrote.
public static class ListingAccess
{
    public static async Task<bool> IsOwnedByAsync(
        IApplicationDbContext dbContext, Listing listing, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id)
        {
            return false;
        }

        if (await IsAuthorAsync(dbContext, listing, id, cancellationToken))
        {
            return true;
        }

        return listing.AgencyId is { } agencyId && await ManagesAgencyAsync(dbContext, agencyId, id, cancellationToken);
    }

    // An Owner or Admin of the agency — who may take one of its listings out of it.
    public static Task<bool> ManagesAgencyAsync(IApplicationDbContext dbContext, Guid agencyId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.AgencyMembers.AnyAsync(
            m => m.AgencyId == agencyId && m.UserId == userId && (m.Role == AgencyRole.Owner || m.Role == AgencyRole.Admin),
            cancellationToken);

    // The account behind the listing's Publisher — the only one who may put it under an agency.
    public static Task<bool> IsAuthorAsync(IApplicationDbContext dbContext, Listing listing, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Publishers.AnyAsync(p => p.Id == listing.PublisherId && p.UserId == userId, cancellationToken);

    public static async Task EnsureCanManageAsync(
        IApplicationDbContext dbContext, Listing listing, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (!isAdmin && !await IsOwnedByAsync(dbContext, listing, userId, cancellationToken))
        {
            throw new ForbiddenAccessException();
        }
    }
}
