import { describe, expect, it } from "vitest";
import type { Listing } from "@/types/listing";
import { arrangeOwnerActions, groupOwnerListings, initialOwnerGroup, matchesOwnerSearch, needsAttention, ownerGroup, parseOwnerSort } from "./ownerGroups";

const now = new Date("2026-10-01T12:00:00Z");
const inDays = (days: number) => new Date(now.getTime() + days * 86_400_000).toISOString();

const listing = (id: string, status: Listing["status"], extra: Partial<Listing> = {}) =>
  ({
    id,
    status,
    expiresAt: null,
    updatedAt: "2026-09-01T00:00:00Z",
    createdAt: "2026-01-01T00:00:00Z",
    price: { priceEur: 0 },
    photos: [{}, {}, {}],
    ...extra,
  }) as Listing;

describe("ownerGroup", () => {
  it("puts every status in one of three groups", () => {
    expect(ownerGroup("Active")).toBe("active");
    for (const s of ["Draft", "PendingReview", "Rejected", "Suspended"] as const) expect(ownerGroup(s)).toBe("unpublished");
    for (const s of ["Expired", "Archived", "Sold", "Rented"] as const) expect(ownerGroup(s)).toBe("ended");
  });
});

describe("needsAttention", () => {
  it("is what waits on the owner", () => {
    expect(needsAttention(listing("a", "Rejected"), now)).toBe(true);
    expect(needsAttention(listing("a", "Suspended"), now)).toBe(true);
    expect(needsAttention(listing("a", "Draft"), now)).toBe(true);
    expect(needsAttention(listing("a", "Active", { expiresAt: inDays(3) }), now)).toBe(true);
    expect(needsAttention(listing("a", "Active", { expiresAt: inDays(60) }), now)).toBe(false);
    expect(needsAttention(listing("a", "PendingReview"), now)).toBe(false);
    expect(needsAttention(listing("a", "Sold"), now)).toBe(false);
  });
});

describe("groupOwnerListings", () => {
  it("groups, then puts what needs attention first, then the most recently changed", () => {
    const groups = groupOwnerListings(
      [
        listing("old", "Active", { expiresAt: inDays(60), updatedAt: "2026-08-01T00:00:00Z" }),
        listing("new", "Active", { expiresAt: inDays(60), updatedAt: "2026-09-20T00:00:00Z" }),
        listing("expiring", "Active", { expiresAt: inDays(2), updatedAt: "2026-07-01T00:00:00Z" }),
        listing("review", "PendingReview"),
        listing("rejected", "Rejected", { updatedAt: "2026-06-01T00:00:00Z" }),
        listing("sold", "Sold"),
      ],
      now,
    );

    expect(groups.active.map((l) => l.id)).toEqual(["expiring", "new", "old"]);
    expect(groups.unpublished.map((l) => l.id)).toEqual(["rejected", "review"]);
    expect(groups.ended.map((l) => l.id)).toEqual(["sold"]);
  });
});

describe("initialOwnerGroup", () => {
  const groups = (active: number, unpublished: number, ended: number) => ({
    active: Array(active).fill(0),
    unpublished: Array(unpublished).fill(0),
    ended: Array(ended).fill(0),
  });

  it("opens the tab in the URL, else Active, else the first with listings", () => {
    expect(initialOwnerGroup("ended", groups(3, 1, 1))).toBe("ended");
    expect(initialOwnerGroup(undefined, groups(3, 1, 1))).toBe("active");
    expect(initialOwnerGroup(undefined, groups(0, 2, 1))).toBe("unpublished");
    expect(initialOwnerGroup("nonsense", groups(0, 0, 0))).toBe("active");
  });
});

describe("arrangeOwnerActions", () => {
  it("shows the next step and Edit; the rest go in the menu", () => {
    expect(arrangeOwnerActions(["edit", "renew", "markAsSold", "deactivate"])).toEqual({ primary: "renew", edit: "edit", more: ["markAsSold", "deactivate"] });
    expect(arrangeOwnerActions(["edit", "markAsSold", "deactivate"])).toEqual({ primary: null, edit: "edit", more: ["markAsSold", "deactivate"] });
    expect(arrangeOwnerActions(["editAndResubmit"])).toEqual({ primary: "editAndResubmit", edit: null, more: [] });
    expect(arrangeOwnerActions(["edit", "deactivate", "activate"])).toEqual({ primary: "activate", edit: "edit", more: ["deactivate"] });
  });
});

describe("sorting a tab", () => {
  const items = [
    listing("cheap-old", "Active", { price: { priceEur: 50_000 } as Listing["price"], createdAt: "2026-01-01T00:00:00Z" }),
    listing("pricey-new", "Active", { price: { priceEur: 140_000 } as Listing["price"], createdAt: "2026-09-01T00:00:00Z" }),
    listing("mid", "Active", { price: { priceEur: 90_000 } as Listing["price"], createdAt: "2026-05-01T00:00:00Z" }),
  ];

  it("by newest or by price, either way", () => {
    expect(groupOwnerListings(items, now, "newest").active.map((l) => l.id)).toEqual(["pricey-new", "mid", "cheap-old"]);
    expect(groupOwnerListings(items, now, "priceAsc").active.map((l) => l.id)).toEqual(["cheap-old", "mid", "pricey-new"]);
    expect(groupOwnerListings(items, now, "priceDesc").active.map((l) => l.id)).toEqual(["pricey-new", "mid", "cheap-old"]);
  });

  it("reads the sort from the URL, recommended when missing or unknown", () => {
    expect(parseOwnerSort("priceAsc")).toBe("priceAsc");
    expect(parseOwnerSort(undefined)).toBe("recommended");
    expect(parseOwnerSort("hacker")).toBe("recommended");
  });
});

describe("matchesOwnerSearch", () => {
  const item = {
    title: "Garsonieră mobilată în centru",
    property: { location: { raionName: "Chișinău", localitateName: null, chisinauSectorName: "Botanica", street: "Strada Dacia" } },
  };

  it("matches every word in the title or the place, ignoring case and diacritics", () => {
    expect(matchesOwnerSearch(item, "garsoniera botanica")).toBe(true);
    expect(matchesOwnerSearch(item, "  DACIA ")).toBe(true);
    expect(matchesOwnerSearch(item, "chisinau mobilata")).toBe(true);
  });

  it("needs every word to match", () => {
    expect(matchesOwnerSearch(item, "garsoniera ciocana")).toBe(false);
  });

  it("an empty search matches everything", () => {
    expect(matchesOwnerSearch(item, "   ")).toBe(true);
  });
});
