using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// Ownership goes through the listing's Publisher — a user owns a listing when the Publisher it was
// published under belongs to them (whichever of their Individual/Agency publishers that was).
public static class ListingAccess
{
    public static Task<bool> IsOwnedByAsync(
        IApplicationDbContext dbContext, Listing listing, Guid? userId, CancellationToken cancellationToken) =>
        userId is null
            ? Task.FromResult(false)
            : dbContext.Publishers.AnyAsync(p => p.Id == listing.PublisherId && p.UserId == userId, cancellationToken);

    public static async Task EnsureCanManageAsync(
        IApplicationDbContext dbContext, Listing listing, Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (!isAdmin && !await IsOwnedByAsync(dbContext, listing, userId, cancellationToken))
        {
            throw new ForbiddenAccessException();
        }
    }
}
