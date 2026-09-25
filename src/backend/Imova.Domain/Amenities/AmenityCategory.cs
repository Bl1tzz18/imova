namespace Imova.Domain.Amenities;

// Groups amenities for display (e.g. the House details accordion). General = not tied to one of
// the themed groups (elevator, building heating type, ...). Persisted as its string name.
public enum AmenityCategory
{
    General = 0,
    Comfort = 1,
    Security = 2,
    Leisure = 3,
}
