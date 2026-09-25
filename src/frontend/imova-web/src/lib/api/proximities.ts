import { cachedReferenceList, parseCachedList } from "@/lib/api/referenceList";
import type { Proximity } from "@/types/listing";

// Bump the version whenever the Proximity shape changes (see lib/api/amenities.ts).
const CACHE_KEY = "imova:proximities:v1";

function isProximity(value: unknown): value is Proximity {
  if (typeof value !== "object" || value === null) return false;
  const p = value as Record<string, unknown>;
  return (
    typeof p.id === "string" &&
    typeof p.key === "string" &&
    typeof p.labelRo === "string" &&
    Array.isArray(p.applicablePropertyTypes)
  );
}

export function parseCachedProximities(raw: string | null): Proximity[] | undefined {
  return parseCachedList(raw, isProximity);
}

export const getProximities = cachedReferenceList("/api/v1/proximities", CACHE_KEY, isProximity);
