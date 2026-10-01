"use server";

import { getSessionToken } from "@/lib/auth/session";
import { forwardedForHeader } from "@/lib/auth/clientIp";
import { visitorHeader } from "@/lib/listing/visitor";

async function headersFor(): Promise<Record<string, string>> {
  const token = await getSessionToken();
  return {
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(await forwardedForHeader()),
    ...(await visitorHeader()),
  };
}

export type RevealPhoneResult = { phone: string } | { phone: null; reason: "unavailable" | "tooMany" | "failed" };

// "Arată" on a listing's phone number. The listing page only has the number's shape; the number
// comes from the API one request at a time (rate-limited per visitor IP — forwarded here — and
// counted for the owner's statistics, once a day per visitor). Never throws.
export async function revealListingPhone(listingId: string): Promise<RevealPhoneResult> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  try {
    const res = await fetch(`${apiUrl}/api/v1/listings/${encodeURIComponent(listingId)}/contact/phone`, {
      method: "POST",
      headers: await headersFor(),
      cache: "no-store",
    });
    if (res.status === 429) return { phone: null, reason: "tooMany" };
    if (res.status === 404) return { phone: null, reason: "unavailable" };
    if (!res.ok) return { phone: null, reason: "failed" };
    return { phone: ((await res.json()) as { phone: string }).phone };
  } catch {
    return { phone: null, reason: "failed" };
  }
}

// The listing page was opened (sent once by its script after loading, so prefetches and most bots
// don't count). The API decides whether it counts; nothing to report back.
export async function recordListingView(listingId: string): Promise<void> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  try {
    await fetch(`${apiUrl}/api/v1/listings/${encodeURIComponent(listingId)}/views`, {
      method: "POST",
      headers: await headersFor(),
      cache: "no-store",
    });
  } catch {
    // A missed view isn't worth bothering the visitor about.
  }
}
