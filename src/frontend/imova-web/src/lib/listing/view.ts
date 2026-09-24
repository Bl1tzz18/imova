import type { Listing, Photo } from "@/types/listing";

// The cover image for cards/map popups — the photo flagged primary, falling back to the first
// one for rows that predate IsPrimary being maintained.
export function coverPhoto(listing: Listing): Photo | undefined {
  return listing.photos.find((p) => p.isPrimary) ?? listing.photos[0];
}

// Reads a numeric TypeSpecificAttributes field (e.g. "rooms"), whichever PropertyType schema it
// belongs to — null when the field is absent or not a number.
export function numberAttribute(listing: Listing, key: string): number | null {
  const value = listing.property.typeSpecificAttributes[key];
  return typeof value === "number" ? value : null;
}

// Land is stored in m² but commonly quoted in "ari" in Moldova (1 ar = 100 m²).
export function squareMetersToAri(m2: number): number {
  return Math.round((m2 / 100) * 100) / 100;
}
