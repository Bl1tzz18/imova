using Imova.Contracts.Proximities;
using MediatR;

namespace Imova.Application.Features.Proximities.GetProximities;

public record GetProximitiesQuery : IRequest<List<ProximityDto>>;
