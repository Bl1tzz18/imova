import { getSessionToken } from "@/lib/auth/session";
import type { Agency, AgencyDirectoryPage, MyAgency } from "@/types/agency";
import type { Raion } from "@/lib/api/locations";

function apiUrl(): string {
  return process.env.API_URL ?? "http://localhost:8080";
}

// Server-side only (needs the session token): the agencies the signed-in user belongs to, with
// their role — for the listing form's "Publică ca" and who may manage which listing.
export async function getMyAgencies(token: string): Promise<MyAgency[]> {
  const res = await fetch(`${apiUrl()}/api/v1/users/me/agencies`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch agencies: ${res.status}`);
  }

  return res.json();
}

// The agency behind /agencies/[slug]: the agency, or — for a slug it used before a rename — the
// slug to redirect to, or null (404: unknown, or deactivated for this visitor). Server-side; a
// signed-in member still sees their deactivated agency.
export type AgencyBySlug = { kind: "agency"; agency: Agency } | { kind: "moved"; slug: string } | null;

export async function getAgencyBySlug(slug: string): Promise<AgencyBySlug> {
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl()}/api/v1/agencies/by-slug/${encodeURIComponent(slug)}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
    // The API answers a former slug with 301 + the current slug in the body; the page redirects itself.
    redirect: "manual",
  });

  if (res.status === 404) return null;
  if (res.status === 301) return { kind: "moved", slug: ((await res.json()) as { slug: string }).slug };
  if (!res.ok) throw new Error(`Failed to fetch agency: ${res.status}`);
  return { kind: "agency", agency: (await res.json()) as Agency };
}

// The public directory (/agencies). `query` comes from agencyDirectoryApiQuery. Null when the API fails.
export async function getAgencyDirectory(query: string): Promise<AgencyDirectoryPage | null> {
  const res = await fetch(`${apiUrl()}/api/v1/agencies?${query}`, { cache: "no-store" });
  return res.ok ? ((await res.json()) as AgencyDirectoryPage) : null;
}

// Every raion, for the directory's city filter (server-rendered; the list changes about once a year).
export async function getRaioane(): Promise<Raion[]> {
  const res = await fetch(`${apiUrl()}/api/v1/locations/raioane`, { next: { revalidate: 86400 } });
  return res.ok ? ((await res.json()) as Raion[]) : [];
}
