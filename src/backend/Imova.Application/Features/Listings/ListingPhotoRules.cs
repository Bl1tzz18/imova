using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Every listing shown on IMOVA has at least MinPhotos photos — for private sellers and agencies
// alike (the CSV import will go through the same rule). It's checked wherever a listing is created,
// sent to review or put (back) on the site: create, submit / resubmit, edit while in review or live,
// re-publish, renew, approve. A photo can't be removed from a live or in-review listing if that
// would leave fewer.
//
// Listings that were already live with fewer before the rule existed stay live; their next change
// (an edit, renewing, re-publishing) needs the photos first.
public static class ListingPhotoRules
{
    public const int MinPhotos = 3;

    // While a listing is in one of these, removing a photo may not take it under MinPhotos.
    private static readonly HashSet<ListingStatus> GuardedWhileIn = [ListingStatus.PendingReview, ListingStatus.Active];

    // Editing a listing in one of these needs the photos (it's in review, live, or about to be resubmitted).
    private static readonly HashSet<ListingStatus> EditNeedsPhotosIn =
        [ListingStatus.PendingReview, ListingStatus.Active, ListingStatus.Rejected, ListingStatus.Suspended];

    public static Task<int> CountAsync(IApplicationDbContext dbContext, Guid listingId, CancellationToken cancellationToken) =>
        dbContext.Photos.CountAsync(p => p.ListingId == listingId, cancellationToken);

    public static async Task<bool> HasEnoughAsync(IApplicationDbContext dbContext, Guid listingId, CancellationToken cancellationToken) =>
        await CountAsync(dbContext, listingId, cancellationToken) >= MinPhotos;

    // 400 listing.notEnoughPhotos ({min, count}) when the listing has fewer than MinPhotos.
    public static async Task EnsureEnoughAsync(IApplicationDbContext dbContext, Guid listingId, CancellationToken cancellationToken)
    {
        var count = await CountAsync(dbContext, listingId, cancellationToken);
        if (count < MinPhotos)
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    "Photos",
                    $"A listing needs at least {MinPhotos} photos ({count} so far).",
                    ErrorCodes.ListingNotEnoughPhotos,
                    CodedFailure.Params(("min", MinPhotos), ("count", count))),
            ]);
        }
    }

    public static Task EnsureEnoughForEditAsync(IApplicationDbContext dbContext, Listing listing, CancellationToken cancellationToken) =>
        EditNeedsPhotosIn.Contains(listing.Status) ? EnsureEnoughAsync(dbContext, listing.Id, cancellationToken) : Task.CompletedTask;

    // 400 listing.lastPhotos ({min}) when removing one photo would take a live or in-review listing
    // under MinPhotos. (An older listing already under it can't lose more either.)
    public static async Task EnsureCanRemoveOneAsync(IApplicationDbContext dbContext, Listing listing, CancellationToken cancellationToken)
    {
        if (!GuardedWhileIn.Contains(listing.Status))
        {
            return;
        }

        if (await CountAsync(dbContext, listing.Id, cancellationToken) - 1 < MinPhotos)
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    "Photos",
                    $"A listing on the site keeps at least {MinPhotos} photos; add another before removing this one.",
                    ErrorCodes.ListingLastPhotos,
                    CodedFailure.Params(("min", MinPhotos))),
            ]);
        }
    }
}
