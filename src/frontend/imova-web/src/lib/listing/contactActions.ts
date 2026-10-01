"use server";

import { getSessionToken } from "@/lib/auth/session";
import type { Listing } from "@/types/listing";

// "Arată numărul" on a listing's contact card. The page itself only carries the half-hidden number,
// so the full one isn't in its HTML for scrapers to collect; it's fetched here, on request, from the
// listing's detail view (which applies the owner's "hide my number" choice). Never throws: null when
// there's no number to show or the call failed.
export async function revealListingPhone(listingId: string): Promise<{ phone: string | null }> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  try {
    const res = await fetch(`${apiUrl}/api/v1/listings/${encodeURIComponent(listingId)}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      cache: "no-store",
    });
    if (!res.ok) return { phone: null };
    const listing = (await res.json()) as Listing;
    return { phone: listing.contact?.phone ?? null };
  } catch {
    return { phone: null };
  }
}
