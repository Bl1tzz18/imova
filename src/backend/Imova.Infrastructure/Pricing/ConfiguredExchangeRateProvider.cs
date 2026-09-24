using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.Infrastructure.Pricing;

public sealed class ConfiguredExchangeRateProvider(ExchangeRateOptions options) : IExchangeRateProvider
{
    public decimal GetEurRate(Currency currency) => currency switch
    {
        Currency.EUR => 1m,
        Currency.MDL => options.MdlToEur,
        Currency.USD => options.UsdToEur,
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null),
    };
}
