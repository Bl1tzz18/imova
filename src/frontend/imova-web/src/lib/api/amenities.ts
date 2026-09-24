import { getBrowserApiUrl } from "@/lib/api/media";
import type { Amenity } from "@/types/listing";

// Bump the version whenever the Amenity shape changes: a tab that cached the old shape would
// otherwise keep using it for its whole session (v1 predated `category`, which left the House
// form's amenity sections empty; v2 predated `applicablePropertyTypes`).
const CACHE_KEY = "imova:amenities:v3";
let inFlight: Promise<Amenity[]> | null = null;

function isAmenity(value: unknown): value is Amenity {
  if (typeof value !== "object" || value === null) return false;
  const a = value as Record<string, unknown>;
  return (
    typeof a.id === "string" &&
    typeof a.key === "string" &&
    typeof a.labelRo === "string" &&
    typeof a.category === "string" &&
    Array.isArray(a.applicablePropertyTypes)
  );
}

// A cached list is only reused if it's a non-empty list of the current Amenity shape — anything
// else (old shape, corrupt JSON, empty) is treated as a cache miss and refetched.
export function parseCachedAmenities(raw: string | null): Amenity[] | undefined {
  if (!raw) return undefined;
  try {
    const parsed: unknown = JSON.parse(raw);
    return Array.isArray(parsed) && parsed.length > 0 && parsed.every(isAmenity) ? parsed : undefined;
  } catch {
    return undefined;
  }
}

function readCache(): Amenity[] | undefined {
  try {
    return parseCachedAmenities(sessionStorage.getItem(CACHE_KEY));
  } catch {
    // Storage unavailable — fall through to a real fetch.
    return undefined;
  }
}

// Seeded reference data (changes only with a backend migration) — cached per page instance and
// in sessionStorage for the tab's lifetime, same approach as lib/api/locations.ts.
export function getAmenities(): Promise<Amenity[]> {
  if (inFlight) return inFlight;

  const cached = readCache();
  if (cached) {
    inFlight = Promise.resolve(cached);
    return inFlight;
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
