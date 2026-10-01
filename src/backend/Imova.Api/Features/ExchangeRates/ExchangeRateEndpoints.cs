using Imova.Application.Features.ExchangeRates.GetExchangeRates;
using MediatR;

namespace Imova.Api.Features.ExchangeRates;

public static class ExchangeRateEndpoints
{
    public static void MapExchangeRateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/exchange-rates", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetExchangeRatesQuery(), cancellationToken)));
    }
}
