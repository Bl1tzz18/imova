using Imova.Domain.Listings;

namespace Imova.Application.Common.Interfaces;

// Source of the rates Price.Create uses to compute Price.PriceEur. Currently backed by fixed,
// configurable rates (see ConfiguredExchangeRateProvider) rather than a live feed.
public interface IExchangeRateProvider
{
    // How many EUR one unit of `currency` is worth — always exactly 1 for EUR.
    decimal GetEurRate(Currency currency);
}
