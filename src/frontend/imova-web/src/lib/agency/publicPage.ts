import { PROPERTY_TYPES, type PropertyTypeName } from "@/lib/property/attributeSchema";
import { truncateText } from "@/lib/listing/seo";
import type { Agency } from "@/types/agency";

// The public agency pages — /agencies (the directory) and /agencies/[slug] (one agency and its
// active listings). Pure (Vitest-covered): their URLs, what their query strings may hold, and what
// the agency looks like outside the site (link previews, Google's structured data).

export const AGENCY_LISTINGS_PAGE_SIZE = 24;
export const AGENCY_DIRECTORY_PAGE_SIZE = 24;

// A short description is how much of the bio a meta description carries (Google shows ~155).
const META_DESCRIPTION_LENGTH = 155;

// Longer than this and the page shows the bio folded, with "Citește mai mult".
const BIO_FOLD_LENGTH = 280;
const BIO_FOLD_LINES = 4;

type Params = Record<string, string | string[] | undefined>;

function first(value: string | string[] | undefined): string | undefined {
  return (Array.isArray(value) ? value[0] : value)?.trim() || undefined;
}

function pageNumber(value: string | string[] | undefined): number {
  const n = Number(first(value));
  return Number.isInteger(n) && n > 1 ? n : 1;
}

function query(pairs: [string, string | number | undefined][]): string {
  const params = new URLSearchParams();
  for (const [key, value] of pairs) {
    if (value !== undefined && value !== "") params.set(key, String(value));
  }
  const text = params.toString();
  return text ? `?${text}` : "";
}

export function agencyPath(slug: string): string {
  return `/agencies/${encodeURIComponent(slug)}`;
}

// --- One agency's listings ---

export const AGENCY_LISTING_SORTS = ["Newest", "PriceAsc", "PriceDesc"] as const;
export type AgencyListingSort = (typeof AGENCY_LISTING_SORTS)[number];

export type AgencyListingsState = {
  transactionType?: "Sale" | "Rent";
  propertyType?: PropertyTypeName;
  sort: AgencyListingSort;
  page: number;
};

// Anything the page doesn't know is dropped rather than refused: a hand-edited URL still shows the
// agency, just unfiltered.
export function parseAgencyListingsParams(params: Params): AgencyListingsState {
  const transactionType = first(params.transactionType);
  const propertyType = first(params.propertyType);
  const sort = first(params.sort);
  return {
    transactionType: transactionType === "Sale" || transactionType === "Rent" ? transactionType : undefined,
    propertyType: (PROPERTY_TYPES as readonly string[]).includes(propertyType ?? "") ? (propertyType as PropertyTypeName) : undefined,
    sort: (AGENCY_LISTING_SORTS as readonly string[]).includes(sort ?? "") ? (sort as AgencyListingSort) : "Newest",
    page: pageNumber(params.page),
  };
}

// The page's own URL for a state; the defaults (Newest, page 1) are left out.
export function agencyListingsHref(slug: string, state: Partial<AgencyListingsState>): string {
  return (
    agencyPath(slug) +
    query([
      ["transactionType", state.transactionType],
      ["propertyType", state.propertyType],
      ["sort", state.sort === "Newest" ? undefined : state.sort],
      ["page", state.page && state.page > 1 ? state.page : undefined],
    ])
  );
}

// The same listings through the search endpoint (GET /api/v1/listings/search?agencyId=…).
export function agencyListingsSearchQuery(agencyId: string, state: AgencyListingsState, pageSize = AGENCY_LISTINGS_PAGE_SIZE): string {
  return query([
    ["agencyId", agencyId],
    ["transactionType", state.transactionType],
    ["propertyType", state.propertyType],
    ["sort", state.sort],
    ["page", state.page],
    ["pageSize", pageSize],
  ]).slice(1);
}

// Whether any filter (not the sort or the page) narrows the listings — "no listings" then means
// "none match", not "the agency has none".
export function hasListingFilters(state: AgencyListingsState): boolean {
  return state.transactionType !== undefined || state.propertyType !== undefined;
}

// --- The directory ---

export type AgencyDirectoryState = {
  q?: string;
  raionId?: string;
  verified: boolean;
  page: number;
};

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function parseAgencyDirectoryParams(params: Params): AgencyDirectoryState {
  const q = first(params.q)?.slice(0, 100);
  const raionId = first(params.raionId);
  const verified = first(params.verified);
  return {
    q: q || undefined,
    raionId: raionId && GUID.test(raionId) ? raionId : undefined,
    verified: verified === "1" || verified === "true",
    page: pageNumber(params.page),
  };
}

export function agencyDirectoryHref(state: Partial<AgencyDirectoryState>): string {
  return (
    "/agencies" +
    query([
      ["q", state.q],
      ["raionId", state.raionId],
      ["verified", state.verified ? "1" : undefined],
      ["page", state.page && state.page > 1 ? state.page : undefined],
    ])
  );
}

export function agencyDirectoryApiQuery(state: AgencyDirectoryState, pageSize = AGENCY_DIRECTORY_PAGE_SIZE): string {
  return query([
    ["q", state.q],
    ["raionId", state.raionId],
    ["verified", state.verified ? "true" : undefined],
    ["page", state.page],
    ["pageSize", pageSize],
  ]).slice(1);
}

// --- How the agency looks ---

export function bioIsLong(bio: string): boolean {
  return bio.length > BIO_FOLD_LENGTH || bio.split("\n").length > BIO_FOLD_LINES;
}

// The bio's opening (whitespace flattened, cut on a word), else the fallback sentence.
export function agencyMetaDescription(bio: string | null, fallback: string): string {
  return bio?.trim() ? truncateText(bio, META_DESCRIPTION_LENGTH) : fallback;
}

// Only http(s) websites become links (the API already refuses anything else); shown without the scheme.
export function websiteLabel(website: string): string {
  return website.replace(/^https?:\/\//i, "").replace(/\/$/, "");
}

// schema.org RealEstateAgent. No telephone: the public only gets the number by asking for it.
export function agencyJsonLd(agency: Agency, url: string): Record<string, unknown> {
  const address =
    agency.raionName || agency.address
      ? {
          address: {
            "@type": "PostalAddress",
            addressCountry: "MD",
            ...(agency.raionName ? { addressRegion: agency.raionName } : {}),
            ...(agency.address ? { streetAddress: agency.address } : {}),
          },
        }
      : {};

  return {
    "@context": "https://schema.org",
    "@type": "RealEstateAgent",
    name: agency.name,
    url,
    ...(agency.logoUrl ? { logo: agency.logoUrl, image: agency.logoUrl } : {}),
    ...(agency.bio ? { description: truncateText(agency.bio, 500) } : {}),
    email: agency.email,
    ...(agency.website ? { sameAs: [agency.website] } : {}),
    ...address,
  };
}
