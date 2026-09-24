using Imova.Domain.Common;

namespace Imova.Domain.Favorites;

// A user saving a listing to view later — a plain join row, not an aggregate root (no invariants
// beyond its own fields, same shape as PropertyLocation).
public sealed class Favorite : Entity
{
    private Favorite(Guid id, Guid userId, Guid listingId) : base(id)
    {
        UserId = userId;
        ListingId = listingId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public Guid ListingId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Favorite Create(Guid userId, Guid listingId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("ListingId is required.", nameof(listingId));
        }

        return new Favorite(Guid.NewGuid(), userId, listingId);
    }
}
