using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Imova.Api.Features.Listings.SearchListings;

// Query string of GET /api/v1/listings/search — the same names the /search page puts in its URL.
// Multi-value filters repeat the parameter (propertyType=Apartment&propertyType=House). The
// TypeSpecificAttributes filters (minRooms, heatingSystem, gasSupply, …) aren't listed here: any
// field of the attributes schema can be filtered, so AttributeFilterParser reads them off the query.
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
    int? MinYearBuilt,
    int? MaxYearBuilt,
    [FromQuery(Name = "condition")] PropertyCondition[]? Conditions,
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
        app.MapGet("/api/v1/listings/search", async ([AsParameters] SearchListingsRequest r, HttpRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
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
                MinYearBuilt = r.MinYearBuilt,
                MaxYearBuilt = r.MaxYearBuilt,
                Conditions = r.Conditions ?? [],
                AttributeFilters = AttributeFilterParser.Parse(
                    request.Query.Select(kv => new KeyValuePair<string, string?[]>(kv.Key, kv.Value.ToArray()))),
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
