using Imova.Application.Features.ExchangeRates.GetExchangeRates;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.ExchangeRates;

public class GetExchangeRatesHandlerTests
{
    [Fact]
    public async Task GivesEveryListingCurrencysRate_EuroBeingOne()
    {
        var dto = await new GetExchangeRatesHandler(new FakeExchangeRateProvider()).Handle(new GetExchangeRatesQuery(), CancellationToken.None);

        Assert.Equal(new Dictionary<string, decimal> { ["EUR"] = 1m, ["MDL"] = 0.05m, ["USD"] = 0.9m }, dto.EurPerUnit);
    }
}
