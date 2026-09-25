using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Imova.Api.Features.Listings.SearchListings;

// Query string of GET /api/v1/listings/search — the same names the /cauta page puts in its URL.
// Multi-value filters repeat the parameter (propertyType=Apartment&propertyType=House).
public record SearchListingsRequest(
    TransactionType? TransactionType,
    [FromQuery(Name = "propertyType")] PropertyType[]? PropertyTypes,
    decimal? MinPriceEur,
    decimal? MaxPriceEur,
    Guid? RaionId,
    Guid? LocalitateId,
    Guid? ChisinauSectorId,
    decimal? MinAreaM2,
    decimal? MaxAreaM2,
    [FromQuery(Name = "amenityIds")] Guid[]? AmenityIds,
    [FromQuery(Name = "proximityIds")] Guid[]? ProximityIds,
    int? MinRooms,
    int? MaxRooms,
    int? MinFloor,
    int? MaxFloor,
    int? MinBathrooms,
    decimal? MinLandAreaM2,
    decimal? MaxLandAreaM2,
    [FromQuery(Name = "housingStockType")] HousingStockType[]? HousingStockTypes,
    [FromQuery(Name = "layout")] ApartmentLayout[]? Layouts,
    [FromQuery(Name = "heatingSystem")] HeatingSystem[]? HeatingSystems,
    [FromQuery(Name = "houseType")] HouseType[]? HouseTypes,
    [FromQuery(Name = "plotType")] PlotType[]? PlotTypes,
    [FromQuery(Name = "locationContext")] LocationContext[]? LocationContexts,
    [FromQuery(Name = "roadAccess")] RoadAccess[]? RoadAccesses,
    [FromQuery(Name = "spaceType")] CommercialSpaceType[]? SpaceTypes,
    [FromQuery(Name = "parkingType")] ParkingType[]? ParkingTypes,
    [FromQuery(Name = "bathroomType")] BathroomType[]? BathroomTypes,
    bool? PetsAllowed,
    bool? UtilitiesIncluded,
    int? MaxLeasePeriodMonths,
    ListingSort? Sort,
    int? Page,
    int? PageSize);

public static class SearchListingsEndpoint
{
    public static void MapSearchListings(this IEndpointRouteBuilder app)
    {
        // Public; a signed-in caller additionally gets IsSaved on each result.
        app.MapGet("/api/v1/listings/search", async ([AsParameters] SearchListingsRequest r, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            var query = new SearchListingsQuery
            {
                TransactionType = r.TransactionType,
                PropertyTypes = r.PropertyTypes ?? [],
                MinPriceEur = r.MinPriceEur,
                MaxPriceEur = r.MaxPriceEur,
                RaionId = r.RaionId,
                LocalitateId = r.LocalitateId,
                ChisinauSectorId = r.ChisinauSectorId,
                MinAreaM2 = r.MinAreaM2,
                MaxAreaM2 = r.MaxAreaM2,
                AmenityIds = r.AmenityIds ?? [],
                ProximityIds = r.ProximityIds ?? [],
                MinRooms = r.MinRooms,
                MaxRooms = r.MaxRooms,
                MinFloor = r.MinFloor,
                MaxFloor = r.MaxFloor,
                MinBathrooms = r.MinBathrooms,
                MinLandAreaM2 = r.MinLandAreaM2,
                MaxLandAreaM2 = r.MaxLandAreaM2,
                HousingStockTypes = r.HousingStockTypes ?? [],
                Layouts = r.Layouts ?? [],
                HeatingSystems = r.HeatingSystems ?? [],
                HouseTypes = r.HouseTypes ?? [],
                PlotTypes = r.PlotTypes ?? [],
                LocationContexts = r.LocationContexts ?? [],
                RoadAccesses = r.RoadAccesses ?? [],
                SpaceTypes = r.SpaceTypes ?? [],
                ParkingTypes = r.ParkingTypes ?? [],
                BathroomTypes = r.BathroomTypes ?? [],
                PetsAllowed = r.PetsAllowed,
                UtilitiesIncluded = r.UtilitiesIncluded,
                MaxLeasePeriodMonths = r.MaxLeasePeriodMonths,
                Sort = r.Sort ?? ListingSort.Newest,
                Page = r.Page ?? 1,
                PageSize = r.PageSize ?? SearchFilterRules.DefaultPageSize,
                CurrentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : null,
            };
            return Results.Ok(await sender.Send(query, cancellationToken));
        });
    }
}
