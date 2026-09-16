export type FieldRequirement = "hidden" | "optional" | "required";

export type DetailFieldName =
  | "area"
  | "rooms"
  | "bathrooms"
  | "floor"
  | "totalFloors"
  | "yearBuilt"
  | "furnished"
  | "parkingAvailable"
  | "petsAllowed";

const APARTMENT = "Apartment";
const HOUSE = "House";
const LAND = "Land";
const COMMERCIAL = "Commercial";
const GARAGE = "Garage";
const ROOM = "Room";

// Mirrors Imova.Domain.Properties.PropertyFieldRules on the backend, which is the
// server-side source of truth (this only drives the form's UX — the backend still
// validates independently). Keep the two in sync.
export function getFieldRequirement(
  field: DetailFieldName,
  propertyType: string,
  listingType: string,
): FieldRequirement {
  switch (field) {
    case "area":
      return propertyType === ROOM ? "optional" : "required";
    case "rooms":
      if (propertyType === APARTMENT || propertyType === HOUSE) return "required";
      if (propertyType === COMMERCIAL) return "optional";
      return "hidden";
    case "bathrooms":
      return [APARTMENT, HOUSE, COMMERCIAL].includes(propertyType) ? "optional" : "hidden";
    case "floor":
      if (propertyType === APARTMENT) return "required";
      if ([COMMERCIAL, GARAGE, ROOM].includes(propertyType)) return "optional";
      return "hidden";
    case "totalFloors":
      if (propertyType === APARTMENT || propertyType === HOUSE) return "required";
      if ([COMMERCIAL, ROOM].includes(propertyType)) return "optional";
      return "hidden";
    case "yearBuilt":
      return [LAND, ROOM].includes(propertyType) ? "hidden" : "optional";
    case "furnished":
      return [APARTMENT, HOUSE, ROOM].includes(propertyType) ? "optional" : "hidden";
    case "parkingAvailable":
      return [APARTMENT, HOUSE, COMMERCIAL].includes(propertyType) ? "optional" : "hidden";
    case "petsAllowed":
      if (listingType !== "Rent") return "hidden";
      return [APARTMENT, HOUSE, ROOM].includes(propertyType) ? "optional" : "hidden";
  }
}
