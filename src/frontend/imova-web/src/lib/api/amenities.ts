import { cachedReferenceList, parseCachedList } from "@/lib/api/referenceList";
import type { Amenity } from "@/types/listing";

// Bump the version whenever the Amenity shape changes: a tab that cached the old shape would
// otherwise keep using it for its whole session (v1 predated `category`, which left the House
// form's amenity sections empty; v2 predated `applicablePropertyTypes`). v4 dropped the
// near_water/near_forest amenities (now proximities), v5 the "guarded" one.
const CACHE_KEY = "imova:amenities:v5";

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

export function parseCachedAmenities(raw: string | null): Amenity[] | undefined {
  return parseCachedList(raw, isAmenity);
}

export const getAmenities = cachedReferenceList("/api/v1/amenities", CACHE_KEY, isAmenity);
