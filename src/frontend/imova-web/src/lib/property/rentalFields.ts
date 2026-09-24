// Which step of the listing form asks each RentalDetails field. All of them are posted as
// "rental.<field>" into Listing.RentalDetails (see formPayload.ts) — this only decides where the
// input appears. Only a rental has these fields at all. (Furnishing isn't a rental term: it's the
// property's "furnished" amenity, for sale and rent alike.)

export type RentalField = "petsAllowed" | "minLeasePeriodMonths" | "securityDepositAmount" | "availableFrom" | "utilitiesIncluded";

export type RentalFieldStep = "details" | "priceTerms";

export const RENTAL_FIELDS_BY_STEP: Record<RentalFieldStep, readonly RentalField[]> = {
  // Step 2 "Details": house rules for the home (its own "Rental rules" section).
  details: ["petsAllowed"],
  // Step 4 "Price & terms": the money and lease terms.
  priceTerms: ["minLeasePeriodMonths", "securityDepositAmount", "availableFrom", "utilitiesIncluded"],
};

// Pets are only a question for somewhere people live — mirrors RentalDetails.PetsApplyTo.
export function petsApplyTo(propertyType: string): boolean {
  return propertyType === "Apartment" || propertyType === "House" || propertyType === "Room";
}

export function rentalFieldsForStep(
  step: RentalFieldStep,
  transactionType: string,
  propertyType: string,
): readonly RentalField[] {
  if (transactionType !== "Rent") return [];
  return RENTAL_FIELDS_BY_STEP[step].filter((field) => field !== "petsAllowed" || petsApplyTo(propertyType));
}

export function rentalInputName(field: RentalField): string {
  return `rental.${field}`;
}
