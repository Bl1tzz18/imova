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

// Static reference data that basically never changes — cached in-memory per page session so the
// cascading selector doesn't refetch on every Raion pick or re-render.
let raioanePromise: Promise<Raion[]> | null = null;
const localitatiByRaionId = new Map<string, Promise<Localitate[]>>();

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
