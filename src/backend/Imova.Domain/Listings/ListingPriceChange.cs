using Imova.Domain.Common;

namespace Imova.Domain.Listings;

// One edit of a listing's asking price: what it was and what it became, each in the currency the
// publisher used plus its EUR equivalent at that moment (Price.PriceEur), so a later "price
// reduced" badge or price chart can compare across currencies. Only written by an edit (never on
// create) and only when the amount or the currency changed — toggling "negotiable", or PriceEur
// moving because exchange rates did, is not a price change. Recorded whatever the listing's status
// (a draft's edits too); readers decide which ones matter.
public sealed class ListingPriceChange : Entity
{
    // For EF Core materialization only.
    private ListingPriceChange()
        : base(Guid.Empty)
    {
    }

    private ListingPriceChange(Guid id, Guid listingId, Price oldPrice, Price newPrice, DateTimeOffset changedAt)
        : base(id)
    {
        ListingId = listingId;
        OldAmount = oldPrice.Amount;
        OldCurrency = oldPrice.Currency;
        OldPriceEur = oldPrice.PriceEur;
        NewAmount = newPrice.Amount;
        NewCurrency = newPrice.Currency;
        NewPriceEur = newPrice.PriceEur;
        ChangedAt = changedAt;
    }

    public Guid ListingId { get; private set; }

    public decimal OldAmount { get; private set; }

    public Currency OldCurrency { get; private set; }

    public decimal OldPriceEur { get; private set; }

    public decimal NewAmount { get; private set; }

    public Currency NewCurrency { get; private set; }

    public decimal NewPriceEur { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    // Null when the asking price is the same (same amount in the same currency).
    public static ListingPriceChange? Between(Guid listingId, Price oldPrice, Price newPrice, DateTimeOffset changedAt)
    {
        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("ListingId is required.", nameof(listingId));
        }

        ArgumentNullException.ThrowIfNull(oldPrice);
        ArgumentNullException.ThrowIfNull(newPrice);

        if (oldPrice.Amount == newPrice.Amount && oldPrice.Currency == newPrice.Currency)
        {
            return null;
        }

        return new ListingPriceChange(Guid.NewGuid(), listingId, oldPrice, newPrice, changedAt);
    }
}
