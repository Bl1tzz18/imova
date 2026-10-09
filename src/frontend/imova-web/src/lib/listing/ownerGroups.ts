import type { Listing } from "@/types/listing";
import { normalizeForSearch } from "@/lib/utils/search";
import { listingExpiry } from "@/lib/listing/expiry";
import { needsPhotos, type OwnerAction } from "@/lib/listing/ownerActions";

// "Anunțurile mele" in three questions an owner actually asks — is it live, what isn't (yet), what's
// over — instead of one tab per status. Pure (Vitest-covered).
export const OWNER_GROUPS = ["active", "unpublished", "ended"] as const;
export type OwnerGroup = (typeof OWNER_GROUPS)[number];

export function ownerGroup(status: Listing["status"]): OwnerGroup {
  switch (status) {
    case "Active":
      return "active";
    case "Draft":
    case "PendingReview":
    case "Rejected":
    case "Suspended":
      return "unpublished";
    default:
      return "ended"; // Expired, Archived, Sold, Rented
  }
}

// Waiting on the owner: fix a rejected/suspended listing, submit a draft, renew before it expires,
// add the photos an older listing is short of.
export function needsAttention(listing: Pick<Listing, "status" | "expiresAt" | "photos">, now: Date): boolean {
  if (listing.status === "Rejected" || listing.status === "Suspended" || listing.status === "Draft") return true;
  if (needsPhotos(listing)) return true;
  const expiry = listingExpiry(listing, now);
  return expiry.kind === "active" && expiry.renewable;
}

type Groupable = Pick<Listing, "status" | "expiresAt" | "updatedAt" | "createdAt" | "photos"> & { price: Pick<Listing["price"], "priceEur"> };

// How a tab is ordered. "recommended": what needs the owner first, then the most recently changed.
export const OWNER_SORTS = ["recommended", "newest", "priceAsc", "priceDesc"] as const;
export type OwnerSort = (typeof OWNER_SORTS)[number];

export function parseOwnerSort(value: string | undefined): OwnerSort {
  return (OWNER_SORTS as readonly string[]).includes(value ?? "") ? (value as OwnerSort) : "recommended";
}

function compare<T extends Groupable>(sort: OwnerSort, now: Date): (a: T, b: T) => number {
  const time = (iso: string) => new Date(iso).getTime();
  switch (sort) {
    case "newest":
      return (a, b) => time(b.createdAt) - time(a.createdAt);
    case "priceAsc":
      return (a, b) => a.price.priceEur - b.price.priceEur;
    case "priceDesc":
      return (a, b) => b.price.priceEur - a.price.priceEur;
    default:
      return (a, b) =>
        Number(needsAttention(b, now)) - Number(needsAttention(a, now)) || time(b.updatedAt) - time(a.updatedAt);
  }
}

export function groupOwnerListings<T extends Groupable>(listings: T[], now: Date, sort: OwnerSort = "recommended"): Record<OwnerGroup, T[]> {
  const groups: Record<OwnerGroup, T[]> = { active: [], unpublished: [], ended: [] };
  for (const listing of listings) groups[ownerGroup(listing.status)].push(listing);
  for (const group of OWNER_GROUPS) groups[group].sort(compare<T>(sort, now));
  return groups;
}

// From this many listings on, "Anunțurile mele" offers search and sorting; below, they'd be clutter.
export const OWNER_TOOLS_FROM = 6;

type Searchable = Pick<Listing, "title"> & {
  property: { location: Pick<NonNullable<Listing["property"]["location"]>, "raionName" | "localitateName" | "chisinauSectorName" | "street"> | null };
};

// The owner's own search: every word must appear in the title or the place (raion, locality,
// neighborhood, street), ignoring case and diacritics — "garsoniera botanica" finds "Garsonieră …,
// Botanica".
export function matchesOwnerSearch(listing: Searchable, query: string): boolean {
  const words = normalizeForSearch(query).split(/\s+/).filter(Boolean);
  if (words.length === 0) return true;
  const location = listing.property.location;
  const haystack = normalizeForSearch(
    [listing.title, location?.raionName, location?.localitateName, location?.chisinauSectorName, location?.street]
      .filter(Boolean)
      .join(" "),
  );
  return words.every((w) => haystack.includes(w));
}

// The tab to open: the one in the URL, else Active, else the first with anything in it.
export function initialOwnerGroup(requested: string | undefined, groups: Record<OwnerGroup, unknown[]>): OwnerGroup {
  if (requested && (OWNER_GROUPS as readonly string[]).includes(requested)) return requested as OwnerGroup;
  if (groups.active.length > 0) return "active";
  return OWNER_GROUPS.find((g) => groups[g].length > 0) ?? "active";
}

// How a listing's actions are laid out: the next step as the filled button (if there is one), Edit
// beside it, and the rarely used rest (sold/rented, deactivate) in the "⋯" menu.
export function arrangeOwnerActions(actions: OwnerAction[]): { primary: OwnerAction | null; edit: OwnerAction | null; more: OwnerAction[] } {
  const primary =
    actions.find((a) => a === "addPhotos" || a === "editAndResubmit" || a === "submitForReview" || a === "renew" || a === "activate") ?? null;
  const edit = primary === "editAndResubmit" || primary === "addPhotos" ? null : (actions.find((a) => a === "edit") ?? null);
  const more = actions.filter((a) => a !== primary && a !== edit && a !== "edit" && a !== "editAndResubmit");
  return { primary, edit, more };
}
