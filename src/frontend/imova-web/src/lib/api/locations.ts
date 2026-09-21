import { getBrowserApiUrl } from "@/lib/api/media";

export type Raion = {
  id: string;
  code: string;
  nameRo: string;
  nameRu: string | null;
  localityLabel: "Localitate" | "Sector";
};

export type Localitate = {
  id: string;
  code: string;
  nameRo: string;
  nameRu: string | null;
};

export type ChisinauSector = {
  id: string;
  name: string;
};

// Static reference data that basically never changes (Moldova's CUATM classification is updated
// roughly once a year via legislation) — cached two ways: in-memory (dedupes concurrent fetches
// within one page instance, e.g. StepTypeLocation's two effects firing together) and in
// sessionStorage (survives hard reloads/re-navigations for the tab's lifetime, so reopening the
// listing form doesn't refetch). sessionStorage's own session-scoped lifetime already gives us
// "cache for the browser session" for free, with no manual TTL/staleness bookkeeping needed.
const inFlight = new Map<string, Promise<unknown>>();

function readSessionCache<T>(key: string): T | undefined {
  try {
    const raw = sessionStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : undefined;
  } catch {
    // Corrupt JSON, private-browsing storage restrictions, etc. — fall through to a real fetch.
    return undefined;
  }
}

function writeSessionCache(key: string, value: unknown): void {
  try {
    sessionStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Quota exceeded / storage disabled — caching is an optimization, not a requirement.
  }
}

function cachedFetch<T>(key: string, url: string, errorLabel: string): Promise<T> {
  const existing = inFlight.get(key) as Promise<T> | undefined;
  if (existing) {
    return existing;
  }

  const cached = readSessionCache<T>(key);
  if (cached !== undefined) {
    const resolved = Promise.resolve(cached);
    inFlight.set(key, resolved);
    return resolved;
  }

  const promise = fetch(url)
    .then((res) => {
      if (!res.ok) throw new Error(`Failed to load ${errorLabel} (${res.status})`);
      return res.json() as Promise<T>;
    })
    .then((data) => {
      writeSessionCache(key, data);
      return data;
    })
    .catch((err: unknown) => {
      // Don't let a failed fetch poison the in-memory cache forever — the next call should retry.
      inFlight.delete(key);
      throw err;
    });

  inFlight.set(key, promise);
  return promise;
}

export function getRaioane(): Promise<Raion[]> {
  return cachedFetch<Raion[]>("locations:raioane", `${getBrowserApiUrl()}/api/v1/locations/raioane`, "raioane");
}

export function getLocalitati(raionId: string): Promise<Localitate[]> {
  const key = `locations:raioane:${raionId}:localitati`;
  return cachedFetch<Localitate[]>(
    key,
    `${getBrowserApiUrl()}/api/v1/locations/raioane/${raionId}/localitati`,
    "localitati",
  );
}

// Fixed list of 9 informal real-estate neighborhood names for Chișinău — independent of
// getLocalitati (which returns Chișinău's real CUATM suburb towns/sectors, a separate field).
export function getChisinauSectors(): Promise<ChisinauSector[]> {
  return cachedFetch<ChisinauSector[]>(
    "locations:chisinau-sectors",
    `${getBrowserApiUrl()}/api/v1/locations/chisinau-sectors`,
    "chisinau sectors",
  );
}
