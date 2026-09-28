using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Microsoft.AspNetCore.WebUtilities;

namespace Imova.Application.Features.Listings.SearchListings;

// The /search page's query string → SearchListingsQuery. The one parser behind the search
// endpoints and saved searches (stored as that same query string), so a saved search always means
// exactly what the page showed. Parameter names are case-insensitive, multi-value filters repeat
// the name (propertyType=Apartment&propertyType=House), attribute filters are read by
// AttributeFilterParser, and a value that doesn't parse is a validation error (400).
public static class SearchQueryString
{
    // Where the user is in the results, not what they searched for — never part of a saved search.
    private static readonly HashSet<string> NotPartOfTheSearch = new(StringComparer.OrdinalIgnoreCase) { "page", "pageSize", "view" };

    public static SearchListingsQuery Parse(string queryString) => Parse(ToPairs(queryString));

    public static SearchListingsQuery Parse(IEnumerable<KeyValuePair<string, string?[]>> query)
    {
        var pairs = query.ToList();
        var reader = new Reader(pairs);

        var result = new SearchListingsQuery
        {
            TransactionType = reader.Enum<TransactionType>("transactionType"),
            PropertyTypes = reader.Enums<PropertyType>("propertyType"),
            MinPriceEur = reader.Decimal("minPriceEur"),
            MaxPriceEur = reader.Decimal("maxPriceEur"),
            RaionId = reader.Guid("raionId"),
            LocalitateId = reader.Guid("localitateId"),
            ChisinauSectorId = reader.Guid("chisinauSectorId"),
            MinAreaM2 = reader.Decimal("minAreaM2"),
            MaxAreaM2 = reader.Decimal("maxAreaM2"),
            AmenityIds = reader.Guids("amenityIds"),
            ProximityIds = reader.Guids("proximityIds"),
            MinYearBuilt = reader.Int("minYearBuilt"),
            MaxYearBuilt = reader.Int("maxYearBuilt"),
            Conditions = reader.Enums<PropertyCondition>("condition"),
            PetsAllowed = reader.Bool("petsAllowed"),
            UtilitiesIncluded = reader.Bool("utilitiesIncluded"),
            MaxLeasePeriodMonths = reader.Int("maxLeasePeriodMonths"),
            Sort = reader.Enum<ListingSort>("sort") ?? ListingSort.Newest,
            Page = reader.Int("page") ?? 1,
            PageSize = reader.Int("pageSize") ?? SearchFilterRules.DefaultPageSize,
        };

        reader.ThrowIfInvalid();
        return result with { AttributeFilters = AttributeFilterParser.Parse(pairs) };
    }

    // The canonical form a saved search is stored in: paging and empty values dropped, parameters
    // sorted — so the same search saved twice compares equal.
    public static string Normalize(string queryString)
    {
        var parts = ToPairs(queryString)
            .Where(kv => !NotPartOfTheSearch.Contains(kv.Key))
            .SelectMany(kv => kv.Value.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => (Key: kv.Key, Value: v!.Trim())))
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Value, StringComparer.Ordinal)
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");

        return string.Join('&', parts.Distinct());
    }

    private static IEnumerable<KeyValuePair<string, string?[]>> ToPairs(string queryString) =>
        QueryHelpers.ParseQuery(queryString).Select(kv => new KeyValuePair<string, string?[]>(kv.Key, kv.Value.ToArray()));

    private sealed class Reader(IEnumerable<KeyValuePair<string, string?[]>> query)
    {
        private readonly Dictionary<string, List<string>> _values = query
            .GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(kv => kv.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).ToList(),
                StringComparer.OrdinalIgnoreCase);

        private readonly List<ValidationFailure> _failures = [];

        public void ThrowIfInvalid()
        {
            if (_failures.Count > 0) throw new ValidationException(_failures);
        }

        // Enum values by name only (case-insensitive) — not "3" or "1,2".
        public IReadOnlyList<T> Enums<T>(string key) where T : struct, Enum =>
            ParseAll(key, value => System.Enum.GetNames<T>().FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase)) is { } name
                ? (true, System.Enum.Parse<T>(name))
                : (false, default));

        public T? Enum<T>(string key) where T : struct, Enum => First(Enums<T>(key));

        public decimal? Decimal(string key) =>
            First(ParseAll(key, value => (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d), d)));

        public int? Int(string key) =>
            First(ParseAll(key, value => (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i), i)));

        public bool? Bool(string key) => First(ParseAll(key, value => (bool.TryParse(value, out var b), b)));

        public Guid? Guid(string key) => First(Guids(key));

        public IReadOnlyList<Guid> Guids(string key) => ParseAll(key, value => (System.Guid.TryParse(value, out var g), g));

        private static T? First<T>(IReadOnlyList<T> values) where T : struct => values.Count > 0 ? values[0] : null;

        private List<T> ParseAll<T>(string key, Func<string, (bool Ok, T Value)> parse)
        {
            var parsed = new List<T>();
            foreach (var value in _values.GetValueOrDefault(key) ?? [])
            {
                var (ok, result) = parse(value);
                if (ok) parsed.Add(result);
                else _failures.Add(new ValidationFailure(key, $"'{value}' is not a valid {key}."));
            }

            return parsed;
        }
    }
}
