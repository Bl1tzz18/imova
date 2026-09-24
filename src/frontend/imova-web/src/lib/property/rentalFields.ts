// Which step of the listing form asks each RentalDetails field. All of them are still posted as
// "rental.<field>" into Listing.RentalDetails (see formPayload.ts) — this only decides where the
// input appears. Only a rental has these fields at all.

export type RentalField =
  | "furnishedStatus"
  | "petsAllowed"
  | "minLeasePeriodMonths"
  | "securityDepositAmount"
  | "availableFrom"
  | "utilitiesIncluded";

export type RentalFieldStep = "details" | "priceTerms";

export const RENTAL_FIELDS_BY_STEP: Record<RentalFieldStep, readonly RentalField[]> = {
  // Step 2 "Details": about the home as it's offered.
  details: ["furnishedStatus", "petsAllowed"],
  // Step 4 "Price & terms": the money and lease terms.
  priceTerms: ["minLeasePeriodMonths", "securityDepositAmount", "availableFrom", "utilitiesIncluded"],
};

export function rentalFieldsForStep(step: RentalFieldStep, transactionType: string): readonly RentalField[] {
  return transactionType === "Rent" ? RENTAL_FIELDS_BY_STEP[step] : [];
}

export function rentalInputName(field: RentalField): string {
  return `rental.${field}`;
}
