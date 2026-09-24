using Imova.Domain.Common;

namespace Imova.Domain.Amenities;

// Seeded reference data (see AmenityConfiguration's HasData), not a hardcoded enum, so the list
// can grow with a migration instead of a code change to every switch over it. Key is the stable
// identifier the frontend translates; LabelRo is the canonical display label.
public sealed class Amenity : Entity
{
    public Amenity(Guid id, string key, string labelRo)
        : base(id)
    {
        Key = key;
        LabelRo = labelRo;
    }

    public string Key { get; private set; }

    public string LabelRo { get; private set; }
}
