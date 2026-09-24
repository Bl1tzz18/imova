using Imova.Domain.Listings;
using Imova.Infrastructure.Pricing;

namespace Imova.UnitTests.Pricing;

public class ConfiguredExchangeRateProviderTests
{
    [Fact]
    public void GetEurRate_ReturnsOneForEurAndTheConfiguredRateOtherwise()
    {
        var provider = new ConfiguredExchangeRateProvider(new ExchangeRateOptions { MdlToEur = 0.05m, UsdToEur = 0.9m });

        Assert.Equal(1m, provider.GetEurRate(Currency.EUR));
        Assert.Equal(0.05m, provider.GetEurRate(Currency.MDL));
        Assert.Equal(0.9m, provider.GetEurRate(Currency.USD));
    }

    [Fact]
    public void Defaults_MatchTheRatesTheDataMigrationUsed()
    {
        // SplitPropertyIntoPropertyListingPublisher hardcodes these for migrated MDL/USD listings.
        var options = new ExchangeRateOptions();

        Assert.Equal(0.051m, options.MdlToEur);
        Assert.Equal(0.86m, options.UsdToEur);
    }
}
