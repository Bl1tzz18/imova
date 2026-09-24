using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;

namespace Imova.Application.Features.Listings.Attributes;

// The single place PropertyAttributes are converted to/from JSON — the API request body, the
// response DTO, and the JSONB column all go through here, so the three can't drift apart.
//
// Wire format (requests/responses): camelCase keys, enums as strings, and no type discriminator
// — the PropertyType sent alongside it decides which schema applies.
// Storage format (JSONB): the same, plus a "kind" discriminator (the PropertyType name), because
// an EF value converter only ever sees the column, never the row's PropertyType.
public static class PropertyAttributesJson
{
    public static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        // A field that belongs to another PropertyType's schema (e.g. "rooms" on Land) must be
        // rejected, not silently dropped. Also applies to nested objects like "utilities".
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly JsonSerializerOptions StorageOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Postgres' jsonb doesn't preserve key order, so the discriminator may not come first.
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddStorageDiscriminator } },
    };

    private static readonly IReadOnlyDictionary<PropertyType, IReadOnlySet<string>> FieldNamesByType =
        Enum.GetValues<PropertyType>().ToDictionary(
            type => type,
            type => (IReadOnlySet<string>)PropertyAttributes.EmptyFor(type).GetType()
                .GetProperties()
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

    // The camelCase field names the given PropertyType's schema accepts.
    public static IReadOnlySet<string> FieldNamesFor(PropertyType propertyType) => FieldNamesByType[propertyType];

    // A null/absent value parses to the type's empty attributes object — whether that's
    // acceptable (required fields) is PropertyAttributesValidator's call, not the parser's.
    public static bool TryParse(
        PropertyType propertyType,
        JsonElement? json,
        out PropertyAttributes? attributes,
        out string? error)
    {
        attributes = null;
        error = null;

        if (!FieldNamesByType.TryGetValue(propertyType, out var allowedFields))
        {
            error = $"Unknown property type '{propertyType}'.";
            return false;
        }

        if (json is null || json.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            attributes = PropertyAttributes.EmptyFor(propertyType);
            return true;
        }

        if (json.Value.ValueKind != JsonValueKind.Object)
        {
            error = "TypeSpecificAttributes must be a JSON object.";
            return false;
        }

        var unknownFields = json.Value.EnumerateObject()
            .Select(p => p.Name)
            .Where(name => !allowedFields.Contains(name))
            .ToList();
        if (unknownFields.Count > 0)
        {
            error = $"{string.Join(", ", unknownFields.Select(f => $"'{f}'"))} "
                + $"{(unknownFields.Count == 1 ? "is not a field" : "are not fields")} of PropertyType {propertyType}.";
            return false;
        }

        try
        {
            var clrType = PropertyAttributes.EmptyFor(propertyType).GetType();
            attributes = (PropertyAttributes?)json.Value.Deserialize(clrType, WireOptions)
                ?? PropertyAttributes.EmptyFor(propertyType);
            return true;
        }
        catch (JsonException ex)
        {
            error = $"TypeSpecificAttributes is malformed: {ex.Message}";
            return false;
        }
    }

    public static JsonElement ToWireElement(PropertyAttributes attributes) =>
        JsonSerializer.SerializeToElement(attributes, attributes.GetType(), WireOptions);

    public static string ToStorageJson(PropertyAttributes attributes) =>
        JsonSerializer.Serialize(attributes, StorageOptions);

    public static PropertyAttributes FromStorageJson(string json) =>
        JsonSerializer.Deserialize<PropertyAttributes>(json, StorageOptions)
        ?? throw new JsonException("Stored property attributes were null.");

    private static void AddStorageDiscriminator(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(PropertyAttributes))
        {
            return;
        }

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "kind",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };

        foreach (var propertyType in Enum.GetValues<PropertyType>())
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(PropertyAttributes.EmptyFor(propertyType).GetType(), propertyType.ToString()));
        }
    }
}
