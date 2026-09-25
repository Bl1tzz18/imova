using Imova.Domain.Common;
using Imova.Domain.Properties;

namespace Imova.Domain.Amenities;

// Seeded reference data (see AmenityConfiguration's HasData), not a hardcoded enum, so the list
// can grow with a migration instead of a code change to every switch over it. Key is the stable
// identifier the frontend translates; LabelRo is the canonical display label.
public sealed class Amenity : Entity
{
    public const string FurnishedKey = "furnished";

    // For EF Core materialization only.
    private Amenity()
        : base(Guid.Empty)
    {
        Key = null!;
        LabelRo = null!;
        ApplicablePropertyTypes = [];
    }

    public Amenity(
        Guid id,
        string key,
        string labelRo,
        AmenityCategory category = AmenityCategory.General,
        IEnumerable<PropertyType>? applicablePropertyTypes = null)
        : base(id)
    {
        Key = key;
        LabelRo = labelRo;
        Category = category;
        ApplicablePropertyTypes = (applicablePropertyTypes ?? Enum.GetValues<PropertyType>()).Distinct().ToArray();
    }

    public string Key { get; private set; }

    public string LabelRo { get; private set; }

    public AmenityCategory Category { get; private set; }

    // Which property types the amenity makes sense for (a sauna for a House, not a Garage) — the
    // listing form only offers these, and the backend rejects any other.
    public PropertyType[] ApplicablePropertyTypes { get; private set; }

    public bool AppliesTo(PropertyType propertyType) => ApplicablePropertyTypes.Contains(propertyType);
}
