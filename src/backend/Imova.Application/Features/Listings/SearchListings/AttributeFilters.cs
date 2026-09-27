using System.Globalization;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;

namespace Imova.Application.Features.Listings.SearchListings;

public enum AttributeFilterKind
{
    // int/decimal fields: a min/max range.
    Number = 1,

    // enum fields: any of the given values.
    Choice = 2,

    // bool fields: exactly this answer.
    YesNo = 3,
}

// One searchable TypeSpecificAttributes field. Name is the camelCase JSON key it's stored under.
public sealed record AttributeField(string Name, AttributeFilterKind Kind, Type? EnumType);

// A filter on one TypeSpecificAttributes field. Field is always a schema name (AttributeSearchSchema),
// never raw input — ListingSearch puts it into SQL as a JSONB key.
public sealed record AttributeFilter(string Field, AttributeFilterKind Kind)
{
    public decimal? Min { get; init; }

    public decimal? Max { get; init; }

    // Choice: enum names, in their canonical casing (how they're stored).
    public IReadOnlyList<string> Values { get; init; } = [];

    public bool? Value { get; init; }
}

// Every field of every PropertyType's attributes record is searchable — read off the records
// themselves, so a field added to the listing form (and its record) becomes filterable without
// touching search. Free-text fields (Commercial's electricalPower) aren't: nothing sensible to match.
public static class AttributeSearchSchema
{
    private static readonly IReadOnlyDictionary<PropertyType, IReadOnlyDictionary<string, AttributeField>> FieldsByType =
        Enum.GetValues<PropertyType>().ToDictionary(
            type => type,
            type => (IReadOnlyDictionary<string, AttributeField>)PropertyAttributes.EmptyFor(type).GetType()
                .GetProperties()
                .Select(ToField)
                .OfType<AttributeField>()
                .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase));

    // Field name → field, across all types. A name several types share (rooms, floor, gasSupply, …)
    // has the same kind in each of them (a unit test checks).
    public static readonly IReadOnlyDictionary<string, AttributeField> AllFields =
        FieldsByType.Values.SelectMany(fields => fields.Values)
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, AttributeField> FieldsFor(PropertyType type) => FieldsByType[type];

    // The property types whose attributes have this field, in enum order.
    public static IReadOnlyList<PropertyType> TypesWith(string field) =>
        FieldsByType.Where(kv => kv.Value.ContainsKey(field)).Select(kv => kv.Key).Order().ToList();

    private static AttributeField? ToField(System.Reflection.PropertyInfo property)
    {
        var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type.IsEnum) return new AttributeField(name, AttributeFilterKind.Choice, type);
        if (type == typeof(bool)) return new AttributeField(name, AttributeFilterKind.YesNo, null);
        if (type == typeof(int) || type == typeof(decimal)) return new AttributeField(name, AttributeFilterKind.Number, null);
        return null;
    }
}

// Reads attribute filters from a search query string, named after the field: a number field is
// min<Field>/max<Field> (minRooms=2&maxRooms=3), a choice field repeats its name
// (heatingSystem=OwnBoiler&heatingSystem=HeatPump), a yes/no field is <field>=true|false.
// Every other parameter is left alone. A value that doesn't parse is a validation error (400).
public static class AttributeFilterParser
{
    public static IReadOnlyList<AttributeFilter> Parse(IEnumerable<KeyValuePair<string, string?[]>> query)
    {
        var failures = new List<ValidationFailure>();
        var filters = new List<AttributeFilter>();
        var ranges = new Dictionary<string, (decimal? Min, decimal? Max)>();

        foreach (var (key, rawValues) in query)
        {
            var values = rawValues.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).ToList();
            if (values.Count == 0) continue;

            if (AttributeSearchSchema.AllFields.TryGetValue(key, out var field))
            {
                if (field.Kind == AttributeFilterKind.Choice)
                {
                    var names = new List<string>();
                    foreach (var value in values)
                    {
                        if (EnumName(field.EnumType!, value) is { } name) names.Add(name);
                        else failures.Add(new ValidationFailure(field.Name, $"'{value}' is not a valid {field.Name}."));
                    }

                    filters.Add(new AttributeFilter(field.Name, field.Kind) { Values = names.Distinct().ToList() });
                }
                else if (field.Kind == AttributeFilterKind.YesNo)
                {
                    if (bool.TryParse(values[0], out var answer)) filters.Add(new AttributeFilter(field.Name, field.Kind) { Value = answer });
                    else failures.Add(new ValidationFailure(field.Name, $"'{values[0]}' is not true or false."));
                }

                // A bare number field (rooms=2) isn't a filter — number fields are ranges.
                continue;
            }

            if (RangeField(key) is not var (rangeField, isMin)) continue;

            if (!decimal.TryParse(values[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            {
                failures.Add(new ValidationFailure(key, $"'{values[0]}' is not a number."));
                continue;
            }

            var range = ranges.GetValueOrDefault(rangeField.Name);
            ranges[rangeField.Name] = isMin ? range with { Min = number } : range with { Max = number };
        }

        if (failures.Count > 0) throw new ValidationException(failures);

        filters.AddRange(ranges.Select(kv => new AttributeFilter(kv.Key, AttributeFilterKind.Number) { Min = kv.Value.Min, Max = kv.Value.Max }));
        return filters;
    }

    // minRooms → (rooms, true); maxLandAreaM2 → (landAreaM2, false); anything else → null.
    private static (AttributeField Field, bool IsMin)? RangeField(string key)
    {
        if (key.Length <= 3) return null;
        var prefix = key[..3].ToLowerInvariant();
        if (prefix is not ("min" or "max")) return null;
        return AttributeSearchSchema.AllFields.TryGetValue(key[3..], out var field) && field.Kind == AttributeFilterKind.Number
            ? (field, prefix == "min")
            : null;
    }

    // By name only — Enum.TryParse would also accept "3" or "1,2".
    private static string? EnumName(Type enumType, string value) =>
        Enum.GetNames(enumType).FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
}
