using Imova.Contracts.Locations;
using MediatR;

namespace Imova.Application.Features.Locations.GetRaioane;

public record GetRaioaneQuery : IRequest<List<RaionDto>>;
