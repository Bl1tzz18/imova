"use server";

import { getSessionToken } from "@/lib/auth/session";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import type { ListingReportReason, ReportListingResult } from "@/types/listingReport";

export type ReportListingActionResult = { data: ReportListingResult; error?: never } | { data?: never; error: string; signedOut?: boolean };

// Reports a listing to the admins. Never throws: a failed call comes back as a translated error
// (signedOut when the session ended meanwhile, so the dialog can offer to sign in).
export async function reportListing(
  listingId: string,
  reason: ListingReportReason,
  details: string,
): Promise<ReportListingActionResult> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  if (!token) {
    return { error: "", signedOut: true };
  }

  const res = await fetch(`${apiUrl}/api/v1/listings/${listingId}/report`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ reason, details: details.trim() || null }),
  });

  if (res.status === 401) {
    return { error: "", signedOut: true };
  }

  if (!res.ok) {
    return { error: await apiErrorMessage(res) };
  }

  return { data: (await res.json()) as ReportListingResult };
}
