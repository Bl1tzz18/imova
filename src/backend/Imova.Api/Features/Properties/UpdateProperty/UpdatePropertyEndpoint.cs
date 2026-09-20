using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.UpdateProperty;
using MediatR;

namespace Imova.Api.Features.Properties.UpdateProperty;

public static class UpdatePropertyEndpoint
{
    public static void MapUpdateProperty(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/properties/{id:guid}", async (
            Guid id,
            UpdatePropertyRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePropertyCommand(
                id,
                user.GetUserId(),
                user.IsInRole(Roles.Admin),
                request.Title,
                request.Description,
                request.PropertyType,
                request.ListingType,
                request.Price,
                request.Currency,
                request.Country,
                request.RaionId,
                request.LocalitateId,
                request.StreetAddress,
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
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
