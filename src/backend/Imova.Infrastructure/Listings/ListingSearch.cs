using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure.Listings;

// The /cauta search. Plain columns (type, price, area, location, amenities, proximities) are
// filtered with LINQ; TypeSpecificAttributes and RentalDetails are JSONB behind value converters,
// which LINQ can't look inside, so their filters become SQL conditions on those columns
// (Properties/Listings.FromSqlRaw, composed into the same query). Every value is passed as a
// parameter ({0}, {1}, …) — never concatenated into the SQL.
public class ListingSearch(ImovaDbContext dbContext) : IListingSearch
{
    public async Task<(IReadOnlyList<Guid> ListingIds, int TotalCount)> SearchAsync(
        SearchListingsQuery q, CancellationToken cancellationToken)
    {
        var properties = FilterProperties(q);
        var listings = FilterListings(q);

        var query =
            from l in listings
            join p in properties on l.PropertyId equals p.Id
            join loc in dbContext.PropertyLocations on p.LocationId equals loc.Id
            select new { Listing = l, Property = p, Location = loc };

        if (q.RaionId is { } raionId)
        {
            query = query.Where(x => x.Location.RaionId == raionId);
        }

        if (q.LocalitateId is { } localitateId)
        {
            query = query.Where(x => x.Location.LocalitateId == localitateId);
        }

        if (q.ChisinauSectorId is { } sectorId)
        {
            query = query.Where(x => x.Location.ChisinauSectorId == sectorId);
        }

        var total = await query.CountAsync(cancellationToken);

        var sorted = q.Sort switch
        {
            ListingSort.PriceAsc => query.OrderBy(x => x.Listing.Price.PriceEur).ThenByDescending(x => x.Listing.PublishedAt),
            ListingSort.PriceDesc => query.OrderByDescending(x => x.Listing.Price.PriceEur).ThenByDescending(x => x.Listing.PublishedAt),
            ListingSort.AreaDesc => query.OrderByDescending(x => x.Property.TotalAreaM2).ThenByDescending(x => x.Listing.PublishedAt),
            _ => query.OrderByDescending(x => x.Listing.PublishedAt).ThenByDescending(x => x.Listing.CreatedAt),
        };

        // Id last, so pages never overlap or skip rows that tie on the sort key.
        var ids = await sorted
            .ThenBy(x => x.Listing.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => x.Listing.Id)
            .ToListAsync(cancellationToken);

        return (ids, total);
    }

    private IQueryable<Property> FilterProperties(SearchListingsQuery q)
    {
        var sql = new SqlConditions("TypeSpecificAttributes");
        sql.IntRange("rooms", q.MinRooms, q.MaxRooms);
        sql.IntRange("floor", q.MinFloor, q.MaxFloor);
        sql.IntRange("bathrooms", q.MinBathrooms, null);
        sql.DecimalRange("landAreaM2", q.MinLandAreaM2, q.MaxLandAreaM2);
        sql.AnyOf("housingStockType", q.HousingStockTypes);
        sql.AnyOf("layout", q.Layouts);
        sql.AnyOf("heatingSystem", q.HeatingSystems);
        sql.AnyOf("houseType", q.HouseTypes);
        sql.AnyOf("plotType", q.PlotTypes);
        sql.AnyOf("locationContext", q.LocationContexts);
        sql.AnyOf("roadAccess", q.RoadAccesses);
        sql.AnyOf("spaceType", q.SpaceTypes);
        sql.AnyOf("parkingType", q.ParkingTypes);
        sql.AnyOf("bathroomType", q.BathroomTypes);

        // EF1002: only fixed column/key names are interpolated; every value is a {n} parameter.
#pragma warning disable EF1002
        var properties = sql.IsEmpty
            ? dbContext.Properties.AsNoTracking()
            : dbContext.Properties.FromSqlRaw($"SELECT * FROM \"Properties\" WHERE {sql.Where}", sql.Parameters).AsNoTracking();
#pragma warning restore EF1002

        if (q.PropertyTypes.Count > 0)
        {
            var types = q.PropertyTypes.Distinct().ToList();
            properties = properties.Where(p => types.Contains(p.PropertyType));
        }

        if (q.MinAreaM2 is { } minArea)
        {
            properties = properties.Where(p => p.TotalAreaM2 >= minArea);
        }

        if (q.MaxAreaM2 is { } maxArea)
        {
            properties = properties.Where(p => p.TotalAreaM2 <= maxArea);
        }

        // All selected amenities/proximities, counted in a subquery — no join, so a listing matching
        // several of them still appears once.
        if (q.AmenityIds.Count > 0)
        {
            var amenityIds = q.AmenityIds.Distinct().ToList();
            properties = properties.Where(p => p.Amenities.Count(a => amenityIds.Contains(a.AmenityId)) == amenityIds.Count);
        }

        if (q.ProximityIds.Count > 0)
        {
            var proximityIds = q.ProximityIds.Distinct().ToList();
            properties = properties.Where(p => p.Proximities.Count(x => proximityIds.Contains(x.ProximityId)) == proximityIds.Count);
        }

        return properties;
    }

    private IQueryable<Listing> FilterListings(SearchListingsQuery q)
    {
        var sql = new SqlConditions("RentalDetails");
        sql.Bool("petsAllowed", q.PetsAllowed);
        sql.Bool("utilitiesIncluded", q.UtilitiesIncluded);
        if (q.MaxLeasePeriodMonths is { } maxLease)
        {
            // No minimum lease at all also qualifies.
            sql.Raw("((\"RentalDetails\"->>'minLeasePeriodMonths') IS NULL OR (\"RentalDetails\"->>'minLeasePeriodMonths')::int <= {0})", maxLease);
        }

        // Rental terms only exist on rentals; a sale's RentalDetails is null. EF1002: as above.
#pragma warning disable EF1002
        var listings = sql.IsEmpty
            ? dbContext.Listings.AsNoTracking()
            : dbContext.Listings.FromSqlRaw($"SELECT * FROM \"Listings\" WHERE \"RentalDetails\" IS NOT NULL AND {sql.Where}", sql.Parameters).AsNoTracking();
#pragma warning restore EF1002

        listings = listings.Where(l => l.Status == ListingStatus.Active);

        if (q.TransactionType is { } transactionType)
        {
            listings = listings.Where(l => l.TransactionType == transactionType);
        }

        if (q.MinPriceEur is { } minPrice)
        {
            listings = listings.Where(l => l.Price.PriceEur >= minPrice);
        }

        if (q.MaxPriceEur is { } maxPrice)
        {
            listings = listings.Where(l => l.Price.PriceEur <= maxPrice);
        }

        return listings;
    }

    // Builds "cond AND cond …" over one JSONB column with {n}-numbered parameters. Keys are fixed
    // strings from this class, never user input.
    private sealed class SqlConditions(string column)
    {
        private readonly List<string> _conditions = [];
        private readonly List<object> _parameters = [];

        public bool IsEmpty => _conditions.Count == 0;

        public string Where => string.Join(" AND ", _conditions);

        public object[] Parameters => [.. _parameters];

        private string Field(string key) => $"(\"{column}\"->>'{key}')";

        public void IntRange(string key, int? min, int? max)
        {
            if (min is not null) Add($"{Field(key)}::int >= {{0}}", min.Value);
            if (max is not null) Add($"{Field(key)}::int <= {{0}}", max.Value);
        }

        public void DecimalRange(string key, decimal? min, decimal? max)
        {
            if (min is not null) Add($"{Field(key)}::numeric >= {{0}}", min.Value);
            if (max is not null) Add($"{Field(key)}::numeric <= {{0}}", max.Value);
        }

        // Enums are stored as their names (PropertyAttributesJson writes string enums).
        public void AnyOf<TEnum>(string key, IReadOnlyList<TEnum> values)
            where TEnum : struct, Enum
        {
            if (values.Count > 0) Add($"{Field(key)} = ANY({{0}})", values.Select(v => v.ToString()).Distinct().ToArray());
        }

        public void Bool(string key, bool? value)
        {
            if (value is not null) Add($"{Field(key)}::boolean = {{0}}", value.Value);
        }

        public void Raw(string condition, object value) => Add(condition, value);

        // "{0}" in a condition refers to its own value; renumbered here to its place in the list.
        private void Add(string condition, object value)
        {
            _conditions.Add(condition.Replace("{0}", $"{{{_parameters.Count}}}"));
            _parameters.Add(value);
        }
    }
}
