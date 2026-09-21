using Imova.Contracts.Locations;
using MediatR;

namespace Imova.Application.Features.Locations.GetRaionLocalitati;

// null means the RaionId doesn't exist — matches GetPropertyByIdQuery's null-means-404 convention.
public record GetRaionLocalitatiQuery(Guid RaionId) : IRequest<List<LocalitateDto>?>;
