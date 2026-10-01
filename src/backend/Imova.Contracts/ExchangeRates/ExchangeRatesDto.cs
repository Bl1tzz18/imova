namespace Imova.Contracts.ExchangeRates;

// How many EUR one unit of each listing currency is worth (EUR itself: 1) — the same rates the
// backend uses for Price.PriceEur, so a converted price on the site matches what search filters on.
// Keys: EUR | MDL | USD.
public record ExchangeRatesDto(IReadOnlyDictionary<string, decimal> EurPerUnit);
