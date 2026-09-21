// Shared by the create and edit flows (see PropertyForm.tsx) — both submit the exact same field
// set, just to different endpoints (POST vs PUT), so the FormData -> JSON mapping lives in one
// place rather than being duplicated between app/properties/new/actions.ts and
// lib/property/actions.ts.

function optionalNumber(value: FormDataEntryValue | null): number | null {
  if (value === null || value === "") return null;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function optionalBoolean(value: FormDataEntryValue | null): boolean | null {
  if (value === "true") return true;
  if (value === "false") return false;
  return null;
}

export function buildPropertyPayload(formData: FormData) {
  return {
    title: formData.get("title"),
    description: formData.get("description"),
    propertyType: formData.get("propertyType"),
    listingType: formData.get("listingType"),
    price: Number(formData.get("price")),
    currency: formData.get("currency"),
    country: formData.get("country"),
    raionId: formData.get("raionId"),
    localitateId: formData.get("localitateId") || null,
    chisinauSectorId: formData.get("chisinauSectorId") || null,
    streetAddress: formData.get("streetAddress") || null,
    buildingNumber: formData.get("buildingNumber") || null,
    area: optionalNumber(formData.get("area")),
    rooms: optionalNumber(formData.get("rooms")),
    bathrooms: optionalNumber(formData.get("bathrooms")),
    floor: optionalNumber(formData.get("floor")),
    totalFloors: optionalNumber(formData.get("totalFloors")),
    yearBuilt: optionalNumber(formData.get("yearBuilt")),
    furnished: optionalBoolean(formData.get("furnished")),
    parkingAvailable: optionalBoolean(formData.get("parkingAvailable")),
    petsAllowed: optionalBoolean(formData.get("petsAllowed")),
  };
}
