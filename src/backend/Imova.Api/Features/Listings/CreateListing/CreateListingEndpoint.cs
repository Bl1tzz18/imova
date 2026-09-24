using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.CreateListing;
using MediatR;

namespace Imova.Api.Features.Listings.CreateListing;

public static class CreateListingEndpoint
{
    public static void MapCreateListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/listings", async (
            CreateListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateListingCommand(
                request.Id,
                user.GetUserId(),
                request.PublisherId,
                request.PropertyType,
                request.TotalAreaM2,
                request.YearBuilt,
                request.Condition,
                request.TypeSpecificAttributes,
                request.AmenityIds,
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
            return Results.Created($"/api/v1/listings/{listing.Id}", listing);
        }).RequireAuthorization();
    }
}
