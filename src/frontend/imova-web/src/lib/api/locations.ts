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

// Static reference data that basically never changes — cached in-memory per page session so the
// cascading selector doesn't refetch on every Raion pick or re-render.
let raioanePromise: Promise<Raion[]> | null = null;
const localitatiByRaionId = new Map<string, Promise<Localitate[]>>();
let chisinauSectorsPromise: Promise<ChisinauSector[]> | null = null;

export function getRaioane(): Promise<Raion[]> {
  raioanePromise ??= fetch(`${getBrowserApiUrl()}/api/v1/locations/raioane`).then((res) => {
    if (!res.ok) throw new Error(`Failed to load raioane (${res.status})`);
    return res.json();
  });
  return raioanePromise;
}

export function getLocalitati(raionId: string): Promise<Localitate[]> {
  let promise = localitatiByRaionId.get(raionId);
  if (!promise) {
    promise = fetch(`${getBrowserApiUrl()}/api/v1/locations/raioane/${raionId}/localitati`).then((res) => {
      if (!res.ok) throw new Error(`Failed to load localitati (${res.status})`);
      return res.json();
    });
    localitatiByRaionId.set(raionId, promise);
  }
  return promise;
}

// Fixed list of 9 informal real-estate neighborhood names for Chișinău — independent of
// getLocalitati (which returns Chișinău's real CUATM suburb towns/sectors, a separate field).
export function getChisinauSectors(): Promise<ChisinauSector[]> {
  chisinauSectorsPromise ??= fetch(`${getBrowserApiUrl()}/api/v1/locations/chisinau-sectors`).then((res) => {
    if (!res.ok) throw new Error(`Failed to load chisinau sectors (${res.status})`);
    return res.json();
  });
  return chisinauSectorsPromise;
}
