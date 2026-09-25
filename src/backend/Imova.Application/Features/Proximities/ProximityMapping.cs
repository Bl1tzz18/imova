using Imova.Contracts.Proximities;
using Imova.Domain.Proximities;

namespace Imova.Application.Features.Proximities;

public static class ProximityMapping
{
    public static ProximityDto ToDto(this Proximity proximity) => new(
            proximity.Id,
            proximity.Key,
            proximity.LabelRo,
            proximity.ApplicablePropertyTypes.Select(t => t.ToString()).ToList());
}
