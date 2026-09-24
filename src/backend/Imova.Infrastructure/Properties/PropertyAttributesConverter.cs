using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Properties.Attributes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Imova.Infrastructure.Properties;

// Property.TypeSpecificAttributes <-> its JSONB column, via the one shared serializer
// (PropertyAttributesJson) so the stored format can't drift from the wire format.
public sealed class PropertyAttributesConverter()
    : ValueConverter<PropertyAttributes, string>(
        attributes => PropertyAttributesJson.ToStorageJson(attributes),
        json => PropertyAttributesJson.FromStorageJson(json));
