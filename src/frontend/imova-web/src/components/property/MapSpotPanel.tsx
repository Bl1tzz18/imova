"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";
import { Link } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui/Badge";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { cn } from "@/lib/utils/cn";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import { coverPhoto, numberAttribute } from "@/lib/listing/view";
import { unitPrice } from "@/lib/listing/price";
import { formatPercentChange } from "@/lib/listing/priceHistory";
import { availableSorts, priceRange, sharedStreetAddress, sortSpotListings, type SpotSort } from "@/lib/map/spotListings";
import type { Listing } from "@/types/listing";
import { listingPath } from "@/lib/listing/listingUrl";

function BuildingIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d="M4 21V5a1 1 0 0 1 1-1h9a1 1 0 0 1 1 1v16M15 9h4a1 1 0 0 1 1 1v11M3 21h18" />
      <path d="M8 8h3M8 12h3M8 16h3" />
    </svg>
  );
}

// Shown when a cluster is clicked at the map's max zoom and still can't break apart (its listings
// share the same or a near-identical coordinate — usually units in one building; see
// PropertyMapFull's ClusteredPropertyMarkers). Built for comparing them, not browsing: the address
// and the price range up top, a sort (price / area / floor — only the ones that tell these
// listings apart), then one compact row per listing — small photo, price, price per m², and
// rooms · area · floor. Floats over the map's bottom-left corner on a desktop (the zoom buttons are
// top-left, the list of results sits to its left), and is a bottom sheet on a phone. Esc closes it.
export function MapSpotPanel({ listings, onClose }: { listings: Listing[]; onClose: () => void }) {
  const t = useTranslations("MapPage");
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");
  const tCard = useTranslations("PropertyCard");
  const tDetail = useTranslations("PropertyDetail");
  const titleId = useId();
  const panel = useRef<HTMLElement>(null);

  const sorts = useMemo(() => availableSorts(listings), [listings]);
  const [sort, setSort] = useState<SpotSort>("price");
  const sorted = useMemo(() => sortSpotListings(listings, sorts.includes(sort) ? sort : "price"), [listings, sorts, sort]);

  const address = sharedStreetAddress(listings);
  const place = formatLocation(listings[0]?.property.location ?? null);
  const range = priceRange(listings);
  // Only worth saying on every row when the spot mixes them (a flat for sale above a shop for rent).
  const mixedKinds =
    new Set(listings.map((l) => `${l.transactionType}/${l.property.propertyType}`)).size > 1;

  useEffect(() => {
    panel.current?.focus();
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  return (
    <section
      ref={panel}
      tabIndex={-1}
      aria-labelledby={titleId}
      className={cn(
        "absolute inset-x-0 bottom-0 z-[1000] flex max-h-[75%] flex-col rounded-t-3xl bg-white shadow-[0_-16px_40px_-12px_rgba(11,22,32,0.35)] outline-none",
        "lg:inset-x-auto lg:bottom-4 lg:left-4 lg:max-h-[calc(100%-2rem)] lg:w-[380px] lg:rounded-3xl lg:shadow-[0_24px_48px_-16px_rgba(11,22,32,0.4)] lg:ring-1 lg:ring-ink-950/5",
        "motion-safe:animate-[spot-panel-in_220ms_ease-out_both]",
      )}
    >
      <div className="mx-auto mt-2.5 h-1 w-10 shrink-0 rounded-full bg-ink-200 lg:hidden" aria-hidden />

      <header className="flex shrink-0 items-start gap-3 px-5 pb-3 pt-3 lg:pt-5">
        <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-accent-50 text-accent-600">
          <BuildingIcon className="h-5 w-5" />
        </span>
        <div className="min-w-0 flex-1">
          {/* "Same address" only when the listings really give one; pins can also coincide for
              listings in different buildings (e.g. placed at the sector's centre). */}
          <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-accent-600">
            {t(address ? "spotSameAddress" : "spotSameSpot")}
          </p>
          <h2 id={titleId} className="truncate font-display text-lg font-medium leading-tight text-ink-950">
            {address ?? place ?? t("spotSameSpot")}
          </h2>
          {address && place && <p className="mt-0.5 truncate text-sm text-ink-500">{place}</p>}
          <p className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[13px]">
            <span className="font-semibold text-ink-900">{t("spotCount", { count: listings.length })}</span>
            {range && (
              <>
                <span className="h-1 w-1 rounded-full bg-ink-300" aria-hidden />
                <span className="text-ink-600">
                  {formatPrice(range.min.amount, range.min.currency)} – {formatPrice(range.max.amount, range.max.currency)}
                </span>
              </>
            )}
          </p>
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label={t("closePanel")}
          title={t("closePanel")}
          className="-mr-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-ink-500 transition-colors hover:bg-ink-100 hover:text-ink-900"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-4 w-4">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </button>
      </header>

      {sorts.length > 1 && (
        <div className="flex shrink-0 items-center gap-3 px-5 pb-3">
          <span className="text-xs text-ink-500">{t("spotSortLabel")}</span>
          <div role="group" aria-label={t("spotSortLabel")} className="inline-flex rounded-full bg-ink-100/70 p-0.5">
            {sorts.map((s) => (
              <button
                key={s}
                type="button"
                aria-pressed={sort === s}
                onClick={() => setSort(s)}
                className={cn(
                  "rounded-full px-3 py-1.5 text-xs font-medium transition-colors",
                  sort === s ? "bg-white text-ink-950 shadow-sm" : "text-ink-500 hover:text-ink-900",
                )}
              >
                {t(`spotSort.${s}`)}
              </button>
            ))}
          </div>
        </div>
      )}

      <ol className="min-h-0 flex-1 overflow-y-auto overscroll-contain border-t border-ink-100 px-2 py-2">
        {sorted.map((listing) => {
          const cover = coverPhoto(listing);
          const perUnit = unitPrice(listing);
          const reduction = listing.priceReduction;
          const rooms = numberAttribute(listing, "rooms");
          const floor = numberAttribute(listing, "floor");
          const totalFloors = numberAttribute(listing, "totalFloors");
          const facts = [
            rooms != null ? tCard("rooms", { count: rooms }) : null,
            `${listing.property.totalAreaM2} m²`,
            floor != null
              ? totalFloors != null
                ? t("spotFloorOf", { floor, totalFloors })
                : t("spotFloor", { floor })
              : null,
          ].filter(Boolean);

          return (
            <li key={listing.id}>
              <Link
                href={listingPath(listing)}
                className="group flex items-center gap-3.5 rounded-2xl p-2.5 transition-colors hover:bg-ink-50 focus-visible:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-500"
              >
                <span className="relative flex h-[72px] w-[72px] shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-brand-800 to-brand-600">
                  {cover ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={cover.thumbnailUrl} alt="" loading="lazy" className="h-full w-full object-cover" />
                  ) : (
                    <PropertyIcon type={listing.property.propertyType} className="h-7 w-7 text-white/40" />
                  )}
                </span>

                <span className="min-w-0 flex-1">
                  {mixedKinds && (
                    <span className="block truncate text-[11px] font-medium uppercase tracking-wide text-ink-500">
                      {tType(listing.property.propertyType)} · {tListing(listing.transactionType)}
                    </span>
                  )}
                  <span className="flex flex-wrap items-baseline gap-x-2">
                    <span className="text-base font-semibold text-ink-950">
                      {formatPrice(listing.price.amount, listing.price.currency)}
                      {listing.transactionType === "Rent" && (
                        <span className="ml-0.5 text-xs font-normal text-ink-500">{tCard("perMonth")}</span>
                      )}
                    </span>
                    {reduction && (
                      <Badge tone="success" className="px-1.5 py-0 text-[11px]">
                        {formatPercentChange(-reduction.percent)}
                      </Badge>
                    )}
                  </span>
                  {perUnit && (
                    <span className="block text-xs text-ink-500">
                      {tDetail(perUnit.unit === "ar" ? "pricePerAr" : "pricePerSquareMetre", {
                        price: formatPrice(perUnit.amount, perUnit.currency),
                      })}
                    </span>
                  )}
                  <span className="mt-1 block truncate text-[13px] font-medium text-ink-700">{facts.join(" · ")}</span>
                </span>

                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  aria-hidden
                  className="h-4 w-4 shrink-0 text-ink-300 transition-transform group-hover:translate-x-0.5 group-hover:text-accent-500"
                >
                  <path d="m9 18 6-6-6-6" />
                </svg>
              </Link>
            </li>
          );
        })}
      </ol>
    </section>
  );
}
