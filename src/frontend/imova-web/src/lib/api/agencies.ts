import { cache } from "react";
import { getSessionToken } from "@/lib/auth/session";
import type { Agency, AgencyDirectoryPage, AgencyInvitation, AgencyMember, Invitation, MyAgency } from "@/types/agency";
import type { Listing } from "@/types/listing";
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

// --- The management pages (/account/agencies/...): server-side, signed in ---

// GET with the session token: the body, or null on 404 (unknown, or hidden from this account) and
// 403 (not a member). Anything else is an error.
async function getAsMember<T>(path: string): Promise<T | null> {
  const token = await getSessionToken();
  if (!token) return null;
  const res = await fetch(`${apiUrl()}${path}`, { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" });
  if (res.status === 404 || res.status === 403) return null;
  if (!res.ok) throw new Error(`GET ${path} failed: ${res.status}`);
  return (await res.json()) as T;
}

// Cached per request: the management layout and its page both ask for it.
export const getAgencyForMember = cache((id: string) => getAsMember<Agency>(`/api/v1/agencies/${encodeURIComponent(id)}`));

export function getAgencyMembers(id: string): Promise<AgencyMember[] | null> {
  return getAsMember<AgencyMember[]>(`/api/v1/agencies/${encodeURIComponent(id)}/members`);
}

// The open invitations — Owners and Admins only (null for anyone else).
export function getAgencyInvitations(id: string): Promise<AgencyInvitation[] | null> {
  return getAsMember<AgencyInvitation[]>(`/api/v1/agencies/${encodeURIComponent(id)}/invitations`);
}

// Its listings in every status (an Agent gets only theirs).
export function getAgencyListings(id: string): Promise<Listing[] | null> {
  return getAsMember<Listing[]>(`/api/v1/agencies/${encodeURIComponent(id)}/listings`);
}

// The invitations waiting for the signed-in account (its email), newest first.
export async function getMyInvitations(token: string): Promise<Invitation[]> {
  const res = await fetch(`${apiUrl()}/api/v1/users/me/invitations`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`Failed to fetch invitations: ${res.status}`);
  return res.json();
}

// Every raion, for the directory's city filter (server-rendered; the list changes about once a year).
export async function getRaioane(): Promise<Raion[]> {
  const res = await fetch(`${apiUrl()}/api/v1/locations/raioane`, { next: { revalidate: 86400 } });
  return res.ok ? ((await res.json()) as Raion[]) : [];
}
