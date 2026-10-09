using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// What the public may see of a listing: it's Active, and it isn't published under a deactivated
// agency (deactivating an agency hides all of its listings — search, map, its page, similar listings,
// alerts — until it's reactivated; its members still see and manage them). The one rule every public
// read uses, so they can't drift apart.
public static class ListingVisibility
{
    // Translatable to SQL (a correlated EXISTS on Agencies).
    public static IQueryable<Listing> WherePublic(this IQueryable<Listing> listings, IApplicationDbContext dbContext) =>
        listings.Where(l => l.Status == ListingStatus.Active
            && (l.AgencyId == null || dbContext.Agencies.Any(a => a.Id == l.AgencyId && a.Status == AgencyStatus.Active)));

    // The same as WherePublic for one listing already loaded.
    public static async Task<bool> IsPublicAsync(IApplicationDbContext dbContext, Listing listing, CancellationToken cancellationToken) =>
        listing.Status == ListingStatus.Active && !await IsUnderDeactivatedAgencyAsync(dbContext, listing, cancellationToken);

    public static Task<bool> IsUnderDeactivatedAgencyAsync(IApplicationDbContext dbContext, Listing listing, CancellationToken cancellationToken) =>
        listing.AgencyId is { } agencyId
            ? dbContext.Agencies.AnyAsync(a => a.Id == agencyId && a.Status == AgencyStatus.Deactivated, cancellationToken)
            : Task.FromResult(false);
}
