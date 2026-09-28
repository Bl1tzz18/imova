import type { Listing } from "@/types/listing";

// Mirrors the backend's ListingExpiry.ReminderBefore: from this close to the end, the owner has
// had the reminder email and "Anunțurile mele" offers to renew.
export const RENEW_WINDOW_DAYS = 7;

const DAY_MS = 24 * 60 * 60 * 1000;

export type ListingExpiry =
  | { kind: "none" }
  | { kind: "active"; expiresAt: string; daysLeft: number; renewable: boolean };

// An Active listing stays live for 6 months from approval / re-activation / renewal (ExpiresAt);
// only then is there anything to show. daysLeft rounds up, so "expires today" is 1 until it's 0.
export function listingExpiry(listing: Pick<Listing, "status" | "expiresAt">, now: Date): ListingExpiry {
  if (listing.status !== "Active" || !listing.expiresAt) {
    return { kind: "none" };
  }

  const msLeft = new Date(listing.expiresAt).getTime() - now.getTime();
  const daysLeft = Math.max(0, Math.ceil(msLeft / DAY_MS));
  return {
    kind: "active",
    expiresAt: listing.expiresAt,
    daysLeft,
    renewable: msLeft <= RENEW_WINDOW_DAYS * DAY_MS,
  };
}
