using Imova.Domain.Common;

namespace Imova.Domain.Favorites;

// A user saving a listing to view later — a plain join row, not an aggregate root (no invariants
// beyond its own fields, same shape as PropertyLocation).
public sealed class Favorite : Entity
{
    private Favorite(Guid id, Guid userId, Guid propertyId) : base(id)
    {
        UserId = userId;
        PropertyId = propertyId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public Guid PropertyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Favorite Create(Guid userId, Guid propertyId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("PropertyId is required.", nameof(propertyId));
        }

        return new Favorite(Guid.NewGuid(), userId, propertyId);
    }
}
