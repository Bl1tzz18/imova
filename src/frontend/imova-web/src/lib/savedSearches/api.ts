import { getSessionToken } from "@/lib/auth/session";
import type { SavedSearch } from "@/lib/savedSearches/savedSearch";

const apiUrl = () => process.env.API_URL ?? "http://localhost:8080";

// Server-side reads/one-shot calls for saved-search pages and route handlers (not server actions:
// they run during a render or a GET, not from a form).

// The signed-in user's saved searches, newest first; null when not signed in.
export async function getSavedSearches(): Promise<SavedSearch[] | null> {
  const token = await getSessionToken();
  if (!token) return null;
  const res = await fetch(`${apiUrl()}/api/v1/saved-searches`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (res.status === 401) return null;
  if (!res.ok) throw new Error(`Failed to load saved searches (${res.status})`);
  return (await res.json()) as SavedSearch[];
}

// Marks a saved search as viewed; returns it (for its query), or "signedOut" / "notFound".
export async function markSavedSearchViewed(id: string): Promise<SavedSearch | "signedOut" | "notFound"> {
  const token = await getSessionToken();
  if (!token) return "signedOut";
  const res = await fetch(`${apiUrl()}/api/v1/saved-searches/${encodeURIComponent(id)}/viewed`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (res.status === 401) return "signedOut";
  if (!res.ok) return "notFound";
  return (await res.json()) as SavedSearch;
}

// The one-click "stop these emails" link — no sign-in. The search's name when alerts were turned
// off, "gone" when the search no longer exists, "invalid" for a bad link.
export async function unsubscribeSavedSearch(id: string, token: string): Promise<{ name: string } | "gone" | "invalid"> {
  const res = await fetch(`${apiUrl()}/api/v1/saved-searches/unsubscribe`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ id, token }),
    cache: "no-store",
  });
  if (res.status === 404) return "gone";
  if (!res.ok) return "invalid";
  return (await res.json()) as { name: string };
}
