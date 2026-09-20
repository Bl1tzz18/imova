namespace Imova.Domain.Locations;

// Which word the frontend's second-level dropdown should use for a given Raion's localities —
// "Sector" only for Chișinău municipiu (its flattened locality list mixes the 5 real sectors with
// suburban towns/communes administratively part of it), "Localitate" for every other Raion.
public enum LocalityLabel
{
    Localitate = 1,
    Sector = 2
}
