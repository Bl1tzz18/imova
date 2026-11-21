import { numberAttribute } from "@/lib/listing/view";
import type { Listing } from "@/types/listing";

// The map's "same address" panel (MapSpotPanel): listings whose pins sit on one spot — usually
// several units in one building — shown as a comparison list. Pure rules, Vitest-covered.

export type SpotSort = "price" | "area" | "floor";

// "str. Durlești 20/D" when every listing gives the same street and number (compared without case
// or surrounding spaces), the street alone when they agree only on that, otherwise null — the
// panel then falls back to the locality.
export function sharedStreetAddress(listings: Listing[]): string | null {
  const streets = new Set(listings.map((l) => normalize(l.property.location?.street)));
  if (streets.size !== 1) return null;
  const first = listings[0]?.property.location;
  const street = first?.street?.trim();
  if (!street) return null;

  const numbers = new Set(listings.map((l) => normalize(l.property.location?.buildingNumber)));
  const number = first?.buildingNumber?.trim();
  return numbers.size === 1 && number ? `${street} ${number}` : street;
}

// Cheapest and dearest, compared in EUR (the listings may be priced in different currencies) but
// shown in each listing's own price. Null for fewer than two listings or when they all cost the same.
export function priceRange(listings: Listing[]): { min: Listing["price"]; max: Listing["price"] } | null {
  if (listings.length < 2) return null;
  const byEur = [...listings].sort((a, b) => a.price.priceEur - b.price.priceEur);
  const min = byEur[0].price;
  const max = byEur[byEur.length - 1].price;
  return min.priceEur === max.priceEur ? null : { min, max };
}

// Which sorts are worth offering: price always; area only when the areas differ; floor only when
// at least two listings have one and they differ (land, houses and garages have none).
export function availableSorts(listings: Listing[]): SpotSort[] {
  const sorts: SpotSort[] = ["price"];
  if (new Set(listings.map((l) => l.property.totalAreaM2)).size > 1) sorts.push("area");
  const floors = listings.map((l) => numberAttribute(l, "floor")).filter((f): f is number => f != null);
  if (floors.length >= 2 && new Set(floors).size > 1) sorts.push("floor");
  return sorts;
}

// Price and floor go up (cheapest, lowest first), area goes down (biggest first). Ties, and
// listings without a floor (last when sorting by floor), keep a stable order by id.
export function sortSpotListings(listings: Listing[], sort: SpotSort): Listing[] {
  const key = (l: Listing): number => {
    switch (sort) {
      case "price":
        return l.price.priceEur;
      case "area":
        return -l.property.totalAreaM2;
      case "floor":
        return numberAttribute(l, "floor") ?? Number.POSITIVE_INFINITY;
    }
  };
  return [...listings].sort((a, b) => {
    const diff = key(a) - key(b);
    return (Number.isNaN(diff) ? 0 : diff) || a.id.localeCompare(b.id);
  });
}

function normalize(value: string | null | undefined): string {
  return (value ?? "").trim().toLocaleLowerCase("ro");
}
