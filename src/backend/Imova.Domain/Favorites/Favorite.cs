using Imova.Domain.Common;
using Imova.Domain.Listings;

namespace Imova.Domain.Favorites;

// A user saving a listing to view later — a plain join row, not an aggregate root (no invariants
// beyond its own fields, same shape as PropertyLocation).
//
// It also remembers what the user has been told about the listing, for the "your saved listing
// changed" emails (FavoriteAlerts in Application): the price they know (the price when they saved
// it, then the price in the latest email), when the latest price email went out (at most one a
// day), and when they were told the listing ended — after that, nothing more is sent.
public sealed class Favorite : Entity
{
    // At most one price-change email per listing per user in this window; changes inside it are
    // reported together (known price → latest price) once it has passed.
    public static readonly TimeSpan PriceAlertInterval = TimeSpan.FromDays(1);

    private Favorite(Guid id, Guid userId, Guid listingId, DateTimeOffset createdAt) : base(id)
    {
        UserId = userId;
        ListingId = listingId;
        CreatedAt = createdAt;
    }

    public Guid UserId { get; private set; }

    public Guid ListingId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // The price the user knows. Null for a favorite created without one (tests, or saved before
    // price alerts and not backfilled) — the alert job then just records today's price.
    public decimal? KnownPriceAmount { get; private set; }

    public Currency? KnownPriceCurrency { get; private set; }

    public DateTimeOffset? PriceAlertSentAt { get; private set; }

    // When the user was told the listing is no longer available — or, if it had already ended when
    // they saved it, when they saved it (nothing to tell). Ends every alert for this favorite.
    public DateTimeOffset? EndedAlertSentAt { get; private set; }

    public static Favorite Create(
        Guid userId, Guid listingId, Price? currentPrice = null, bool listingHasEnded = false, DateTimeOffset? now = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("ListingId is required.", nameof(listingId));
        }

        var createdAt = now ?? DateTimeOffset.UtcNow;
        return new Favorite(Guid.NewGuid(), userId, listingId, createdAt)
        {
            KnownPriceAmount = currentPrice?.Amount,
            KnownPriceCurrency = currentPrice?.Currency,
            EndedAlertSentAt = listingHasEnded ? createdAt : null,
        };
    }

    public bool AlertsStopped => EndedAlertSentAt is not null;

    public bool KnowsPrice => KnownPriceAmount is not null && KnownPriceCurrency is not null;

    public bool PriceDiffersFrom(Price price) =>
        KnowsPrice && (KnownPriceAmount != price.Amount || KnownPriceCurrency != price.Currency);

    // Whether to email the user now that the price went from the one they know to `current`.
    public bool IsPriceAlertDue(Price current, DateTimeOffset now) =>
        !AlertsStopped
        && PriceDiffersFrom(current)
        && (PriceAlertSentAt is not { } last || now - last >= PriceAlertInterval);

    // `current` is now the price they know: after the email, or silently (no price known yet, or
    // nobody to email — opted out, unconfirmed address — so turning emails back on starts fresh).
    public void RecordPrice(Price current, DateTimeOffset now, bool emailSent)
    {
        KnownPriceAmount = current.Amount;
        KnownPriceCurrency = current.Currency;
        if (emailSent)
        {
            PriceAlertSentAt = now;
        }
    }

    public void RecordEnded(DateTimeOffset now) => EndedAlertSentAt ??= now;
}
