using Imova.Domain.Common;
using Imova.Domain.Properties;

namespace Imova.Domain.Proximities;

// What a property is close to (a school, a park, a bus stop, ...) — seeded reference data like
// Amenity (see ProximityConfiguration's HasData), but kept separate from it: amenities describe
// the property itself, proximities its surroundings. Key is the stable identifier the frontend
// translates; LabelRo is the canonical display label.
public sealed class Proximity : Entity
{
    // For EF Core materialization only.
    private Proximity()
        : base(Guid.Empty)
    {
        Key = null!;
        LabelRo = null!;
        ApplicablePropertyTypes = [];
    }

    public Proximity(Guid id, string key, string labelRo, IEnumerable<PropertyType>? applicablePropertyTypes = null)
        : base(id)
    {
        Key = key;
        LabelRo = labelRo;
        ApplicablePropertyTypes = (applicablePropertyTypes ?? Enum.GetValues<PropertyType>()).Distinct().ToArray();
    }

    public string Key { get; private set; }

    public string LabelRo { get; private set; }

    // Which property types it can be selected for — the listing form only offers these, and the
    // backend rejects any other. Every seeded proximity currently applies to every type.
    public PropertyType[] ApplicablePropertyTypes { get; private set; }

    public bool AppliesTo(PropertyType propertyType) => ApplicablePropertyTypes.Contains(propertyType);
}
