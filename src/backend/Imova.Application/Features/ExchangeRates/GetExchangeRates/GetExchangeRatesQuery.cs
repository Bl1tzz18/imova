using Imova.Contracts.ExchangeRates;
using MediatR;

namespace Imova.Application.Features.ExchangeRates.GetExchangeRates;

public record GetExchangeRatesQuery : IRequest<ExchangeRatesDto>;
