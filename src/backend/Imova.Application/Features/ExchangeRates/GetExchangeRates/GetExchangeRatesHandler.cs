using Imova.Application.Common.Interfaces;
using Imova.Contracts.ExchangeRates;
using Imova.Domain.Listings;
using MediatR;

namespace Imova.Application.Features.ExchangeRates.GetExchangeRates;

// Lets the web app show a price in another currency ("≈ 1 600 000 MDL") with the configured rates.
public class GetExchangeRatesHandler(IExchangeRateProvider rates) : IRequestHandler<GetExchangeRatesQuery, ExchangeRatesDto>
{
    public Task<ExchangeRatesDto> Handle(GetExchangeRatesQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new ExchangeRatesDto(
            Enum.GetValues<Currency>().ToDictionary(c => c.ToString(), rates.GetEurRate)));
}
