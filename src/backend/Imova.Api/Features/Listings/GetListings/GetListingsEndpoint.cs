using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.GetListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Api.Features.Listings.GetListings;

public static class GetListingsEndpoint
{
    public static void MapGetListings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/listings", async (
            string? propertyType,
            string? transactionType,
            decimal? minPriceEur,
            decimal? maxPriceEur,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseOptionalEnum<PropertyType>(propertyType, out var parsedPropertyType))
            {
                return Results.BadRequest($"Unknown property type '{propertyType}'.");
            }

            if (!TryParseOptionalEnum<TransactionType>(transactionType, out var parsedTransactionType))
            {
                return Results.BadRequest($"Unknown transaction type '{transactionType}'.");
            }

            // Public endpoint (no RequireAuthorization) — the current user id is only used, when
            // present, to mark which listings they've already saved (see ListingDto.IsSaved).
            var currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : (Guid?)null;

            var query = new GetListingsQuery(parsedPropertyType, parsedTransactionType, minPriceEur, maxPriceEur, currentUserId);
            return Results.Ok(await sender.Send(query, cancellationToken));
        });
    }

    private static bool TryParseOptionalEnum<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
        {
            return false;
        }

        parsed = result;
        return true;
    }
}
