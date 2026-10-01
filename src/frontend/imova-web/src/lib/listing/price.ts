import type { Listing } from "@/types/listing";

// The price card's extra lines — pure rules (Vitest-covered).

// How many EUR one unit of each currency is worth (GET /api/v1/exchange-rates).
export type EurRates = Record<string, number>;

// "≈ 1 607 800 MDL": the listing's EUR price in another currency, with the backend's own rates.
// Rounded to the nearest 100 above 10 000 (it's an estimate — more digits would only look precise).
// Null without a rate for that currency.
export function convertFromEur(priceEur: number, currency: string, rates: EurRates | null): number | null {
  const rate = rates?.[currency];
  if (!rate || rate <= 0) return null;
  const value = priceEur / rate;
  return value >= 10_000 ? Math.round(value / 100) * 100 : Math.round(value);
}

// The price per unit of area, as buyers compare a sale: per m², except land, which in Moldova is
// priced per ar (100 m²). In the listing's own currency, rounded. Only for a sale with an area.
export type UnitPrice = { amount: number; currency: string; unit: "m2" | "ar" };

export function unitPrice(listing: Pick<Listing, "transactionType" | "price"> & { property: Pick<Listing["property"], "propertyType" | "totalAreaM2"> }): UnitPrice | null {
  const area = listing.property.totalAreaM2;
  if (listing.transactionType !== "Sale" || !area || area <= 0) return null;
  const unit = listing.property.propertyType === "Land" ? "ar" : "m2";
  const units = unit === "ar" ? area / 100 : area;
  return { amount: Math.round(listing.price.amount / units), currency: listing.price.currency, unit };
}
