import { getBrowserApiUrl } from "@/lib/api/media";

// One match of GET /api/v1/locations/search (raioane, localitati and Chișinău sectors).
export type LocationSuggestion = {
  kind: "Raion" | "Localitate" | "Sector";
  id: string;
  name: string;
  raionId: string;
  raionName: string;
};

export async function searchLocations(text: string, signal?: AbortSignal): Promise<LocationSuggestion[]> {
  if (text.trim().length < 2) return [];
  const res = await fetch(`${getBrowserApiUrl()}/api/v1/locations/search?q=${encodeURIComponent(text.trim())}`, { signal });
  return res.ok ? ((await res.json()) as LocationSuggestion[]) : [];
}

// The /cauta location parameters a picked suggestion stands for.
export function locationParams(suggestion: LocationSuggestion): Record<string, string[]> {
  switch (suggestion.kind) {
    case "Raion":
      return { raionId: [suggestion.id] };
    case "Sector":
      return { raionId: [suggestion.raionId], chisinauSectorId: [suggestion.id] };
    default:
      return { raionId: [suggestion.raionId], localitateId: [suggestion.id] };
  }
}
