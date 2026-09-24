namespace Imova.Infrastructure.Pricing;

// Fixed EUR conversion rates used to compute Price.PriceEur. Deliberately static/configurable
// rather than a live feed — PriceEur only needs to be close enough to make listings priced in
// different currencies comparable for filtering/sorting. Override via the "ExchangeRates" config
// section; the defaults are approximate placeholders.
public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    // EUR per 1 MDL.
    public decimal MdlToEur { get; init; } = 0.051m;

    // EUR per 1 USD.
    public decimal UsdToEur { get; init; } = 0.86m;
}
