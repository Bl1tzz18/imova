using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Properties.CreateProperty;
using MediatR;

namespace Imova.Api.Features.Properties.CreateProperty;

public static class CreatePropertyEndpoint
{
    public static void MapCreateProperty(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties", async (
            CreatePropertyRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreatePropertyCommand(
                request.Id,
                user.GetUserId(),
                request.Title,
                request.Description,
                request.PropertyType,
                request.ListingType,
                request.Price,
                request.Currency,
                request.Country,
                request.RaionId,
                request.LocalitateId,
                request.ChisinauSectorId,
                request.StreetAddress,
                request.BuildingNumber,
                request.Area,
                request.Rooms,
                request.Bathrooms,
                request.Floor,
                request.TotalFloors,
                request.YearBuilt,
                request.Furnished,
                request.ParkingAvailable,
                request.PetsAllowed);

            var property = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/v1/properties/{property.Id}", property);
        }).RequireAuthorization();
    }
}
