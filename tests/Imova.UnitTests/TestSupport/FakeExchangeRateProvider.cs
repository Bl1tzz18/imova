using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.UnitTests.TestSupport;

// Round-number rates so expected PriceEur values are easy to read in assertions.
internal sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    public decimal GetEurRate(Currency currency) => currency switch
    {
        Currency.EUR => 1m,
        Currency.MDL => 0.05m,
        Currency.USD => 0.9m,
        _ => throw new ArgumentOutOfRangeException(nameof(currency)),
    };
}
