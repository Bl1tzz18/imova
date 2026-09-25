import { getBrowserApiUrl } from "@/lib/api/media";

// A cached list is only reused if it's a non-empty list of the current item shape — anything else
// (old shape, corrupt JSON, empty) is treated as a cache miss and refetched.
export function parseCachedList<T>(raw: string | null, isItem: (value: unknown) => value is T): T[] | undefined {
  if (!raw) return undefined;
  try {
    const parsed: unknown = JSON.parse(raw);
    return Array.isArray(parsed) && parsed.length > 0 && parsed.every(isItem) ? parsed : undefined;
  } catch {
    return undefined;
  }
}

// Loader for a seeded reference list (changes only with a backend migration) — cached per page
// instance and in sessionStorage for the tab's lifetime, same approach as lib/api/locations.ts.
// Bump the version in cacheKey whenever the item shape changes.
export function cachedReferenceList<T>(
  path: string,
  cacheKey: string,
  isItem: (value: unknown) => value is T,
): () => Promise<T[]> {
  let inFlight: Promise<T[]> | null = null;

  function readCache(): T[] | undefined {
    try {
      return parseCachedList(sessionStorage.getItem(cacheKey), isItem);
    } catch {
      // Storage unavailable — fall through to a real fetch.
      return undefined;
    }
  }

  return () => {
    if (inFlight) return inFlight;

    const cached = readCache();
    if (cached) {
      inFlight = Promise.resolve(cached);
      return inFlight;
    }

    inFlight = fetch(`${getBrowserApiUrl()}${path}`)
      .then((res) => {
        if (!res.ok) throw new Error(`Failed to load ${path} (${res.status})`);
        return res.json() as Promise<T[]>;
      })
      .then((items) => {
        try {
          sessionStorage.setItem(cacheKey, JSON.stringify(items));
        } catch {
          // Caching is an optimization only.
        }
        return items;
      })
      .catch((error) => {
        inFlight = null;
        throw error;
      });

    return inFlight;
  };
}
