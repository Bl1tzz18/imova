using Imova.Domain.Common;

namespace Imova.Domain.Locations;

// Informal real-estate-industry neighborhood names for Chișinău (Botanica, Centru, Râșcani, ...) —
// NOT CUATM entities: CUATM's Chișinău subtree only has 5 official sectors, already flattened
// into Localitate alongside its suburb towns/communes (see CuatmLocationSeeder). This is a
// separate, independent field on PropertyLocation (ChisinauSectorId) — a listing can have a
// Localitate (suburb), a ChisinauSector (informal neighborhood), both, or neither. Seeded once
// from a small hardcoded list (see ChisinauSectorSeeder) since there's no authoritative external
// source for these, unlike Raion/Localitate.
public sealed class ChisinauSector : Entity
{
    private ChisinauSector(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public static ChisinauSector Create(string name) => new(Guid.NewGuid(), name);
}
