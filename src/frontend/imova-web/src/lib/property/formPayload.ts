import { readAttributes } from "@/lib/property/attributeSchema";

// Shared by the create and edit flows (see PropertyForm.tsx) — both submit the same Property +
// Listing field set in one payload, just to different endpoints (POST vs PUT), so the
// FormData -> JSON mapping lives in one place.

function optionalNumber(value: FormDataEntryValue | null): number | null {
  if (value === null || value === "") return null;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function optionalString(value: FormDataEntryValue | null): string | null {
  return typeof value === "string" && value.trim() !== "" ? value : null;
}

function readRentalDetails(formData: FormData) {
  return {
    furnishedStatus: optionalString(formData.get("rental.furnishedStatus")) ?? "Unfurnished",
    minLeasePeriodMonths: optionalNumber(formData.get("rental.minLeasePeriodMonths")),
    securityDepositAmount: optionalNumber(formData.get("rental.securityDepositAmount")),
    availableFrom: optionalString(formData.get("rental.availableFrom")),
    utilitiesIncluded: formData.get("rental.utilitiesIncluded") === "true",
    petsAllowed: formData.get("rental.petsAllowed") === "true",
  };
}

export function buildListingPayload(formData: FormData) {
  const propertyType = String(formData.get("propertyType"));
  const transactionType = formData.get("transactionType");

  return {
    // Property (the physical asset)
    propertyType,
    totalAreaM2: Number(formData.get("totalAreaM2")),
    yearBuilt: optionalNumber(formData.get("yearBuilt")),
    condition: optionalString(formData.get("condition")),
    typeSpecificAttributes: readAttributes(propertyType, formData),
    amenityIds: formData.getAll("amenityIds").filter((id): id is string => typeof id === "string"),
    country: formData.get("country"),
    raionId: formData.get("raionId"),
    localitateId: formData.get("localitateId") || null,
    chisinauSectorId: formData.get("chisinauSectorId") || null,
    streetAddress: formData.get("streetAddress") || null,
    buildingNumber: formData.get("buildingNumber") || null,
    // Listing (the offer)
    transactionType,
    title: formData.get("title"),
    description: formData.get("description"),
    price: Number(formData.get("price")),
    currency: formData.get("currency"),
    isNegotiable: formData.get("isNegotiable") === "true",
    rentalDetails: transactionType === "Rent" ? readRentalDetails(formData) : null,
  };
}
