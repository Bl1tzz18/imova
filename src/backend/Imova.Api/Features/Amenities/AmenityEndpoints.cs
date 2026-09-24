using Imova.Application.Features.Amenities.GetAmenities;
using MediatR;

namespace Imova.Api.Features.Amenities;

public static class AmenityEndpoints
{
    public static void MapAmenityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/amenities", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAmenitiesQuery(), cancellationToken)));
    }
}
