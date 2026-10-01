using Imova.Application.Features.Listings.GetSimilarListings;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure.Listings;

// One stage of "Anunțuri asemănătoare" as a single query: the hard filters and the stage's limits in
// the WHERE, the ranking in the ORDER BY — same area, then same city, then closer price (in 10% steps,
// so the steps below still decide between near-equal prices), then a room count within one (Apartment/
// House), then the newest; the id last keeps it stable. Raw SQL because the room count is inside the
// JSONB attributes. Every value is a {n} parameter; only fixed column names are in the text.
public class SimilarListingsFinder(ImovaDbContext dbContext) : ISimilarListingsFinder
{
    public async Task<IReadOnlyList<Guid>> FindAsync(
        SimilarListingTarget target, SimilarListingStage stage, int limit, CancellationToken cancellationToken)
    {
        var parameters = new List<object>();
        string P(object value)
        {
            parameters.Add(value);
            return $"{{{parameters.Count - 1}}}";
        }

        var where = new List<string>
        {
            $"l.\"Status\" = {P((int)ListingStatus.Active)}",
            $"l.\"TransactionType\" = {P((int)target.TransactionType)}",
            $"p.\"PropertyType\" = {P((int)target.PropertyType)}",
            $"l.\"Id\" <> {P(target.ListingId)}",
        };

        // The listing's own area: its sector, else its locality (null when it names neither).
        var sameArea = target.ChisinauSectorId is { } sectorId
            ? $"loc.\"ChisinauSectorId\" = {P(sectorId)}"
            : target.LocalitateId is { } localitateId
                ? $"loc.\"LocalitateId\" = {P(localitateId)}"
                : null;
        var sameCity = $"loc.\"RaionId\" = {P(target.RaionId)}";

        if (stage.SameAreaOnly && sameArea is not null)
        {
            where.Add(sameArea);
        }

        if (stage.SameCityOnly)
        {
            where.Add(sameCity);
        }

        if (stage.PriceTolerance is { } tolerance && target.PriceEur > 0)
        {
            where.Add($"l.\"PriceEur\" BETWEEN {P(target.PriceEur * (1 - tolerance))} AND {P(target.PriceEur * (1 + tolerance))}");
        }

        var orderBy = new List<string>();
        if (sameArea is not null)
        {
            orderBy.Add($"CASE WHEN {sameArea} THEN 0 ELSE 1 END");
        }

        orderBy.Add($"CASE WHEN {sameCity} THEN 0 ELSE 1 END");
        if (target.PriceEur > 0)
        {
            var price = P(target.PriceEur);
            orderBy.Add($"FLOOR(ABS(l.\"PriceEur\" - {price}) / {price} * 10)");
        }

        if (target.Rooms is { } rooms)
        {
            // Rooms is a whole number in the attributes JSON whenever it's there (PropertyAttributesValidator).
            orderBy.Add($"CASE WHEN ABS((p.\"TypeSpecificAttributes\"->>'rooms')::int - {P(rooms)}) <= 1 THEN 0 ELSE 1 END");
        }

        orderBy.Add("l.\"PublishedAt\" DESC NULLS LAST");
        orderBy.Add("l.\"Id\"");

        var sql = $"""
            SELECT l."Id" AS "Value"
            FROM "Listings" l
            JOIN "Properties" p ON p."Id" = l."PropertyId"
            JOIN "PropertyLocations" loc ON loc."Id" = p."LocationId"
            WHERE {string.Join(" AND ", where)}
            ORDER BY {string.Join(", ", orderBy)}
            LIMIT {P(limit)}
            """;

#pragma warning disable EF1002 // Only fixed column names are interpolated; every value is a parameter.
        return await dbContext.Database.SqlQueryRaw<Guid>(sql, [.. parameters]).ToListAsync(cancellationToken);
#pragma warning restore EF1002
    }
}
