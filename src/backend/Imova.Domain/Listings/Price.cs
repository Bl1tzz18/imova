namespace Imova.Domain.Listings;

// What the publisher asks for, in the currency they chose, plus PriceEur: the same amount
// converted to EUR at save time. PriceEur is what any price filtering/sorting must use, so results
// are comparable regardless of which currency each listing was priced in. It's a cached value —
// recomputed whenever the price is set, not when exchange rates change.
public sealed record Price
{
    // For EF Core materialization only.
    private Price()
    {
    }

    private Price(decimal amount, Currency currency, decimal priceEur, bool isNegotiable)
    {
        Amount = amount;
        Currency = currency;
        PriceEur = priceEur;
        IsNegotiable = isNegotiable;
    }

    public decimal Amount { get; private init; }

    public Currency Currency { get; private init; }

    public decimal PriceEur { get; private init; }

    public bool IsNegotiable { get; private init; }

    // eurRate is how many EUR one unit of `currency` is worth (1 for EUR itself) — supplied by the
    // caller (see IExchangeRateProvider) so the domain stays free of configuration.
    public static Price Create(decimal amount, Currency currency, bool isNegotiable, decimal eurRate)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Price must be greater than zero.");
        }

        if (!Enum.IsDefined(currency))
        {
            throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown currency.");
        }

        if (eurRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eurRate), "Exchange rate must be greater than zero.");
        }

        if (currency == Currency.EUR && eurRate != 1)
        {
            throw new ArgumentException("The EUR-to-EUR exchange rate must be 1.", nameof(eurRate));
        }

        var priceEur = Math.Round(amount * eurRate, 2, MidpointRounding.AwayFromZero);
        return new Price(amount, currency, priceEur, isNegotiable);
    }
}
