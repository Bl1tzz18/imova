using Imova.Contracts.Locations;
using MediatR;

namespace Imova.Application.Features.Locations.GetChisinauSectors;

public record GetChisinauSectorsQuery : IRequest<List<ChisinauSectorDto>>;
