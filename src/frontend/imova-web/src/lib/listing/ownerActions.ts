import type { Listing } from "@/types/listing";
import { listingExpiry } from "@/lib/listing/expiry";

// What the owner can do with their listing, in each status — one list for "Anunțurile mele" and for
// the bar on the listing's own page, mirroring the domain's guards (Listing.SubmitForReview, Renew,
// MarkAsSold/MarkAsRented, Archive, Publish). Pure (Vitest-covered).

export type OwnerAction =
  | "edit"
  | "editAndResubmit"
  | "submitForReview"
  | "renew"
  | "markAsSold"
  | "markAsRented"
  | "deactivate"
  | "activate";

// Ask before these: sold/rented and deactivation take the listing off the site.
export const CONFIRMED_ACTIONS: ReadonlySet<OwnerAction> = new Set(["markAsSold", "markAsRented", "deactivate"]);

const ARCHIVABLE = new Set(["Active", "Rented", "Sold", "Expired"]);
const REPUBLISHABLE = new Set(["Archived", "Expired"]);

export function ownerActions(listing: Pick<Listing, "status" | "transactionType" | "expiresAt">, now: Date): OwnerAction[] {
  const actions: OwnerAction[] = [listing.status === "Rejected" ? "editAndResubmit" : "edit"];
  if (listing.status === "Draft") actions.push("submitForReview");
  const expiry = listingExpiry(listing, now);
  if (expiry.kind === "active" && expiry.renewable) actions.push("renew");
  if (listing.status === "Active") actions.push(listing.transactionType === "Sale" ? "markAsSold" : "markAsRented");
  if (ARCHIVABLE.has(listing.status)) actions.push("deactivate");
  if (REPUBLISHABLE.has(listing.status)) actions.push("activate");
  return actions;
}

// The owner bar's message on the listing page: is it public, and if not why and what next.
export type OwnerNotice =
  | { kind: "active"; expiresAt: string | null; renewable: boolean }
  | { kind: "notPublic"; status: "Draft" | "PendingReview" | "Expired" | "Archived" }
  | { kind: "ended"; status: "Sold" | "Rented" }
  | { kind: "blocked"; status: "Rejected" | "Suspended"; reason: string | null };

export function ownerNotice(
  listing: Pick<Listing, "status" | "expiresAt" | "rejectionReason" | "suspensionReason">,
  now: Date,
): OwnerNotice {
  switch (listing.status) {
    case "Active": {
      const expiry = listingExpiry(listing, now);
      return expiry.kind === "active"
        ? { kind: "active", expiresAt: expiry.expiresAt, renewable: expiry.renewable }
        : { kind: "active", expiresAt: null, renewable: false };
    }
    case "Sold":
    case "Rented":
      return { kind: "ended", status: listing.status };
    case "Rejected":
      return { kind: "blocked", status: "Rejected", reason: listing.rejectionReason };
    case "Suspended":
      return { kind: "blocked", status: "Suspended", reason: listing.suspensionReason };
    case "Draft":
    case "PendingReview":
    case "Expired":
    case "Archived":
      return { kind: "notPublic", status: listing.status };
    default:
      return { kind: "notPublic", status: "Archived" };
  }
}
