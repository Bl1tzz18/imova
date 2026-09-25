using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.UpdateListing;
using MediatR;

namespace Imova.Api.Features.Listings.UpdateListing;

public static class UpdateListingEndpoint
{
    public static void MapUpdateListing(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/listings/{id:guid}", async (
            Guid id,
            UpdateListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateListingCommand(
                id,
                user.GetUserId(),
                user.IsInRole(Roles.Admin),
                request.PropertyType,
                request.TotalAreaM2,
                request.YearBuilt,
                request.Condition,
                request.TypeSpecificAttributes,
                request.AmenityIds,
                request.ProximityIds,
                request.Country,
                request.RaionId,
                request.LocalitateId,
                request.ChisinauSectorId,
                request.StreetAddress,
                request.BuildingNumber,
                request.TransactionType,
                request.Title,
                request.Description,
                request.Price,
                request.Currency,
                request.IsNegotiable,
                request.RentalDetails);

            var listing = await sender.Send(command, cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(listing);
        }).RequireAuthorization();
    }
}
