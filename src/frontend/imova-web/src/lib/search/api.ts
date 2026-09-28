import { getSessionToken } from "@/lib/auth/session";
import { toQueryString, type SearchState } from "@/lib/search/filters";
import type { Listing } from "@/types/listing";

export const SEARCH_PAGE_SIZE = 24;

export type SearchResults = { items: Listing[]; page: number; pageSize: number; totalCount: number };

async function fetchResults(path: string, query: string): Promise<SearchResults | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}${path}?${query}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });
  return res.ok ? ((await res.json()) as SearchResults) : null;
}

// Server-side (the /search page renders its results on the server, so every filtered URL is a
// complete, crawlable page). Signed-in callers get IsSaved on each card. Null when the API fails.
export async function searchListings(state: SearchState): Promise<SearchResults | null> {
  const query = toQueryString(state);
  return fetchResults("/api/v1/listings/search", `${query}${query ? "&" : ""}pageSize=${SEARCH_PAGE_SIZE}`);
}

// The /map view of the same search: every match that has coordinates, in one response (capped by
// the API — totalCount says how many matched in all).
export async function searchMapListings(state: SearchState): Promise<SearchResults | null> {
  return fetchResults("/api/v1/listings/search/map", toQueryString(state));
}
