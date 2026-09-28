using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Media;

// Who may add photos under a listing id. Once the listing exists: its owner (or an admin), same
// as every other listing write. Before it exists — the add-listing form uploads under the id it
// generated for the listing it's about to create — the first uploader claims the id: nobody else
// can add photos under it, and nobody else can create a listing with it (CreateListingHandler).
public static class MediaAccess
{
    public static async Task EnsureCanUploadAsync(
        IApplicationDbContext dbContext, Guid listingId, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);
        if (listing is not null)
        {
            await ListingAccess.EnsureCanManageAsync(dbContext, listing, userId, isAdmin, cancellationToken);
            return;
        }

        await EnsureNoOneElsesPhotosAsync(dbContext, listingId, userId, cancellationToken);
    }

    // Photos already sitting under a not-yet-created listing id must all be the caller's.
    public static async Task EnsureNoOneElsesPhotosAsync(
        IApplicationDbContext dbContext, Guid listingId, Guid userId, CancellationToken cancellationToken)
    {
        var claimedBySomeoneElse = await dbContext.Photos.AnyAsync(
            p => p.ListingId == listingId && (p.UploadedByUserId == null || p.UploadedByUserId != userId),
            cancellationToken);

        if (claimedBySomeoneElse)
        {
            throw new ForbiddenAccessException();
        }
    }
}
