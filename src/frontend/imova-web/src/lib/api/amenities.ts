import { getBrowserApiUrl } from "@/lib/api/media";
import type { Amenity } from "@/types/listing";

const CACHE_KEY = "imova:amenities";
let inFlight: Promise<Amenity[]> | null = null;

// Seeded reference data (changes only with a backend migration) — cached per page instance and
// in sessionStorage for the tab's lifetime, same approach as lib/api/locations.ts.
export function getAmenities(): Promise<Amenity[]> {
  if (inFlight) return inFlight;

  try {
    const cached = sessionStorage.getItem(CACHE_KEY);
    if (cached) {
      inFlight = Promise.resolve(JSON.parse(cached) as Amenity[]);
      return inFlight;
    }
  } catch {
    // Storage unavailable/corrupt — fall through to a real fetch.
  }

  inFlight = fetch(`${getBrowserApiUrl()}/api/v1/amenities`)
    .then((res) => {
      if (!res.ok) throw new Error(`Failed to load amenities (${res.status})`);
      return res.json() as Promise<Amenity[]>;
    })
    .then((amenities) => {
      try {
        sessionStorage.setItem(CACHE_KEY, JSON.stringify(amenities));
      } catch {
        // Caching is an optimization only.
      }
      return amenities;
    })
    .catch((error) => {
      inFlight = null;
      throw error;
    });

  return inFlight;
}
