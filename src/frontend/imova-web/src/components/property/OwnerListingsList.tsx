"use client";

import { useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { Badge } from "@/components/ui/Badge";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { OwnerListingActions } from "@/components/property/OwnerListingActions";
import { ownerActions } from "@/lib/listing/ownerActions";
import { inputClass } from "@/components/ui/Field";
import {
  groupOwnerListings,
  initialOwnerGroup,
  matchesOwnerSearch,
  needsAttention,
  OWNER_GROUPS,
  OWNER_SORTS,
  OWNER_TOOLS_FROM,
  parseOwnerSort,
  type OwnerGroup,
  type OwnerSort,
} from "@/lib/listing/ownerGroups";
import { formatDate, formatLocation, formatPrice } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import { coverPhoto } from "@/lib/listing/view";
import { listingExpiry } from "@/lib/listing/expiry";
import type { Listing } from "@/types/listing";

// "accent" is this app's alert/attention color (same tone used for validation and error
// banners elsewhere), so it's reserved for statuses that need the owner to act — Rejected and
// Suspended — rather than for "live" statuses, so a rejected listing doesn't blend into the
// rest of the gray/neutral bucket.
const statusBadgeTone: Record<string, "brand" | "accent" | "neutral"> = {
  Active: "brand",
  Rented: "brand",
  Sold: "brand",
  Draft: "neutral",
  PendingReview: "neutral",
  Rejected: "accent",
  Suspended: "accent",
  Archived: "neutral",
  Expired: "neutral",
};

// "Anunțurile mele": three tabs — Active, Not published (drafts, in review, rejected, suspended),
// Ended (expired, deactivated, sold, rented) — each with a count and a dot when something in it
// waits on the owner, which is also listed first. From OWNER_TOOLS_FROM listings on, a search (title
// and place, across all three tabs — the counts show where the matches are) and a sort. Tab, search
// and sort are kept in the URL (?tab=&q=&sort=).
export function OwnerListingsList({
  listings,
  initialTab,
  initialQuery = "",
  initialSort,
}: {
  listings: Listing[];
  initialTab?: string;
  initialQuery?: string;
  initialSort?: string;
}) {
  const t = useTranslations("MyListingsPage");
  const now = new Date();
  const showTools = listings.length >= OWNER_TOOLS_FROM;
  const [query, setQuery] = useState(showTools ? initialQuery : "");
  const [sort, setSort] = useState<OwnerSort>(() => parseOwnerSort(initialSort));
  const groups = groupOwnerListings(
    listings.filter((l) => matchesOwnerSearch(l, query)),
    now,
    sort,
  );
  const [tab, setTab] = useState<OwnerGroup>(() => initialOwnerGroup(initialTab, groupOwnerListings(listings, now)));

  // Only the address bar changes — nothing is fetched again.
  function remember(next: { tab?: OwnerGroup; query?: string; sort?: OwnerSort }) {
    const params = new URLSearchParams();
    params.set("tab", next.tab ?? tab);
    const q = (next.query ?? query).trim();
    if (q) params.set("q", q);
    const s = next.sort ?? sort;
    if (s !== "recommended") params.set("sort", s);
    try {
      window.history.replaceState(window.history.state, "", `?${params}`);
    } catch {
      // Storage of the address isn't essential.
    }
  }

  function select(group: OwnerGroup) {
    setTab(group);
    remember({ tab: group });
  }

  const visible = groups[tab];

  return (
    <div>
      {showTools && (
        <div className="mb-3 flex gap-2">
          <label className="relative min-w-0 flex-1">
            <span className="sr-only">{t("searchLabel")}</span>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden>
              <circle cx="11" cy="11" r="6.5" />
              <path d="m20 20-4.2-4.2" strokeLinecap="round" />
            </svg>
            <input
              type="search"
              value={query}
              onChange={(e) => {
                setQuery(e.target.value);
                remember({ query: e.target.value });
              }}
              placeholder={t("searchPlaceholder")}
              className={cn(inputClass, "pl-10")}
            />
          </label>
          {/* The sort: a full menu on wider screens; on a phone a 44px sort icon over an invisible
              native select, so the search keeps the room and the phone's own picker still opens. */}
          <label className="relative flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-ink-200 bg-white text-ink-700 focus-within:border-brand-500 focus-within:ring-2 focus-within:ring-brand-500/20 sm:w-auto sm:border-0 sm:bg-transparent sm:focus-within:ring-0">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-5 w-5 sm:hidden" aria-hidden>
              <path d="M7 4v16M4 17l3 3 3-3M17 20V4M14 7l3-3 3 3" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            <select
              aria-label={t("sortLabel")}
              value={sort}
              onChange={(e) => {
                const next = parseOwnerSort(e.target.value);
                setSort(next);
                remember({ sort: next });
              }}
              className={cn(inputClass, "absolute inset-0 h-full w-full opacity-0 sm:static sm:w-auto sm:pr-8 sm:opacity-100")}
            >
              {OWNER_SORTS.map((s) => (
                <option key={s} value={s}>
                  {t(`sort.${s}`)}
                </option>
              ))}
            </select>
          </label>
        </div>
      )}

      <div role="tablist" aria-label={t("title")} className="grid grid-cols-3 gap-1 rounded-2xl bg-ink-100/70 p-1 sm:max-w-xl">
        {OWNER_GROUPS.map((group) => {
          const selected = tab === group;
          const attention = groups[group].some((l) => needsAttention(l, now));
          return (
            <button
              key={group}
              type="button"
              role="tab"
              id={`owner-tab-${group}`}
              aria-selected={selected}
              aria-controls="owner-tab-panel"
              onClick={() => select(group)}
              className={cn(
                "relative flex min-h-11 flex-col items-center justify-center gap-0.5 rounded-xl px-2 py-1.5 text-center text-sm font-medium leading-tight transition-colors sm:flex-row sm:gap-2",
                selected ? "bg-white text-ink-950 shadow-sm" : "text-ink-600 hover:text-ink-900",
              )}
            >
              <span>{t(`group.${group}`)}</span>
              <span className={cn("text-xs tabular-nums", selected ? "text-ink-500" : "text-ink-400")}>{groups[group].length}</span>
              {attention && (
                <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-accent-500" title={t("needsAttention")}>
                  <span className="sr-only">{t("needsAttention")}</span>
                </span>
              )}
            </button>
          );
        })}
      </div>

      <div id="owner-tab-panel" role="tabpanel" aria-labelledby={`owner-tab-${tab}`}>
        {visible.length > 0 ? (
          <ul className="mt-5 flex flex-col gap-3">
            {visible.map((listing) => (
              <OwnerListingRow key={listing.id} listing={listing} now={now} showStatus={tab !== "active"} />
            ))}
          </ul>
        ) : (
          <div className="mt-6 flex flex-col items-center gap-2 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-14 text-center">
            <p className="text-sm font-medium text-ink-700">
              {listings.length === 0
                ? t("emptyTitle")
                : query.trim()
                  ? t("searchEmpty", { query: query.trim() })
                  : t(`groupEmpty.${tab}`)}
            </p>
            {listings.length === 0 && <p className="text-sm text-ink-500">{t("emptyBody")}</p>}
            {listings.length > 0 && query.trim() && (
              <button
                type="button"
                onClick={() => {
                  setQuery("");
                  remember({ query: "" });
                }}
                className="text-sm font-medium text-brand-700 hover:underline"
              >
                {t("clearSearch")}
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// One listing: photo, title, status + place, price, the line that matters for its status (until
// when it's active, or the moderators' reason), and its actions — beside it on a wide screen, in
// their own row under it on a phone.
// showStatus: off in the Active tab, where every listing is active anyway.
function OwnerListingRow({ listing, now, showStatus }: { listing: Listing; now: Date; showStatus: boolean }) {
  const t = useTranslations("MyListingsPage");
  const locale = useLocale();
  const location = formatLocation(listing.property.location);
  const cover = coverPhoto(listing);
  const expiry = listingExpiry(listing, now);
  const actions = ownerActions(listing, now);
  const reason =
    listing.status === "Rejected" ? listing.rejectionReason : listing.status === "Suspended" ? listing.suspensionReason : null;
  const price = (
    <span className="font-display text-base font-semibold text-ink-950">
      {formatPrice(listing.price.amount, listing.price.currency)}
    </span>
  );

  return (
    <li className="rounded-2xl border border-ink-100 bg-white p-3 sm:p-4">
      <div className="flex gap-3 sm:gap-4">
        <Link
          href={`/property/${listing.id}`}
          className="flex h-16 w-20 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-brand-800 to-brand-600 sm:h-[72px] sm:w-24"
        >
          {cover ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={cover.url} alt="" className="h-full w-full object-cover" />
          ) : (
            <PropertyIcon type={listing.property.propertyType} className="h-7 w-7 text-white/40" />
          )}
        </Link>

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-3">
            <Link
              href={`/property/${listing.id}`}
              className="line-clamp-2 text-sm font-medium leading-5 text-ink-900 hover:underline"
            >
              {listing.title}
            </Link>
            <span className="hidden shrink-0 sm:block">{price}</span>
          </div>

          <div className="mt-1.5 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-ink-500">
            {showStatus && <Badge tone={statusBadgeTone[listing.status] ?? "neutral"}>{statusLabel(t, listing.status)}</Badge>}
            {location && <span className="truncate">{location}</span>}
          </div>

          <p className="mt-1 sm:hidden">{price}</p>

          {expiry.kind === "active" && (
            <p className={cn("mt-1 text-xs", expiry.renewable ? "font-medium text-accent-700" : "text-ink-400")}>
              {expiry.renewable
                ? t("expiresSoon", { date: formatDate(expiry.expiresAt, locale) })
                : t("activeUntil", { date: formatDate(expiry.expiresAt, locale) })}
            </p>
          )}
        </div>

        <OwnerListingActions listingId={listing.id} actions={actions} className="hidden shrink-0 self-center lg:block" />
      </div>

      {reason && (
        <div className="mt-3 rounded-xl border border-accent-100 bg-accent-100/50 px-3.5 py-2.5 text-sm text-accent-700">
          <p className="font-medium">
            {listing.status === "Rejected" ? t("rejectionReasonLabel") : t("suspensionReasonLabel")}
          </p>
          <p className="mt-0.5">{reason}</p>
        </div>
      )}

      <OwnerListingActions listingId={listing.id} actions={actions} stretch className="mt-3 border-t border-ink-100 pt-3 lg:hidden" />
    </li>
  );
}

function statusLabel(t: ReturnType<typeof useTranslations<"MyListingsPage">>, status: string) {
  switch (status) {
    case "Draft":
      return t("statusDraft");
    case "PendingReview":
      return t("statusPendingReview");
    case "Active":
      return t("statusPublished");
    case "Rejected":
      return t("statusRejected");
    case "Suspended":
      return t("statusSuspended");
    case "Rented":
      return t("statusRented");
    case "Sold":
      return t("statusSold");
    case "Archived":
      return t("statusArchived");
    case "Expired":
      return t("statusExpired");
    default:
      return status;
  }
}
