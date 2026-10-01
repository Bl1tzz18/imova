import { describe, expect, it } from "vitest";
import type { Listing } from "@/types/listing";
import { ownerActions, ownerNotice } from "./ownerActions";

const now = new Date("2026-10-01T12:00:00Z");
const inDays = (days: number) => new Date(now.getTime() + days * 24 * 60 * 60 * 1000).toISOString();

const listing = (status: Listing["status"], extra: Partial<Listing> = {}) =>
  ({
    status,
    transactionType: "Sale",
    expiresAt: null,
    rejectionReason: null,
    suspensionReason: null,
    ...extra,
  }) as Listing;

describe("ownerActions", () => {
  it("an active sale: edit, mark as sold, deactivate — renew only in its last week", () => {
    expect(ownerActions(listing("Active", { expiresAt: inDays(90) }), now)).toEqual(["edit", "markAsSold", "deactivate"]);
    expect(ownerActions(listing("Active", { expiresAt: inDays(3) }), now)).toEqual(["edit", "renew", "markAsSold", "deactivate"]);
  });

  it("an active rental is marked as rented, not sold", () => {
    expect(ownerActions(listing("Active", { transactionType: "Rent" }), now)).toContain("markAsRented");
    expect(ownerActions(listing("Active", { transactionType: "Rent" }), now)).not.toContain("markAsSold");
  });

  it("follows each status's guards", () => {
    expect(ownerActions(listing("Draft"), now)).toEqual(["edit", "submitForReview"]);
    expect(ownerActions(listing("PendingReview"), now)).toEqual(["edit"]);
    expect(ownerActions(listing("Rejected"), now)).toEqual(["editAndResubmit"]);
    expect(ownerActions(listing("Suspended"), now)).toEqual(["edit"]);
    expect(ownerActions(listing("Sold"), now)).toEqual(["edit", "deactivate"]);
    expect(ownerActions(listing("Expired"), now)).toEqual(["edit", "deactivate", "activate"]);
    expect(ownerActions(listing("Archived"), now)).toEqual(["edit", "activate"]);
  });
});

describe("ownerNotice", () => {
  it("says until when an active listing is public, and when to renew", () => {
    expect(ownerNotice(listing("Active", { expiresAt: inDays(90) }), now)).toEqual({ kind: "active", expiresAt: inDays(90), renewable: false });
    expect(ownerNotice(listing("Active", { expiresAt: inDays(2) }), now)).toMatchObject({ renewable: true });
  });

  it("gives the moderators' reason for a rejected or suspended listing", () => {
    expect(ownerNotice(listing("Rejected", { rejectionReason: "Poze neclare." }), now)).toEqual({ kind: "blocked", status: "Rejected", reason: "Poze neclare." });
    expect(ownerNotice(listing("Suspended", { suspensionReason: "Duplicat." }), now)).toEqual({ kind: "blocked", status: "Suspended", reason: "Duplicat." });
  });

  it("says when it isn't public, and when it was sold or rented", () => {
    expect(ownerNotice(listing("PendingReview"), now)).toEqual({ kind: "notPublic", status: "PendingReview" });
    expect(ownerNotice(listing("Sold"), now)).toEqual({ kind: "ended", status: "Sold" });
  });
});
