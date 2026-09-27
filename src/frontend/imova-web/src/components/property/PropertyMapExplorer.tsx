"use client";

import dynamic from "next/dynamic";
import { useMemo, useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { ClusterOverflowPanel } from "@/components/property/ClusterOverflowPanel";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { cn } from "@/lib/utils/cn";
import { formatPrice } from "@/lib/utils/format";
import { coverPhoto } from "@/lib/listing/view";
import type { Listing } from "@/types/listing";
import type { MapPoint } from "@/components/property/PropertyMapFull";

const PropertyMapFull = dynamic(
  () => import("@/components/property/PropertyMapFull").then((m) => m.PropertyMapFull),
  { ssr: false, loading: () => <div className="h-full w-full animate-pulse bg-ink-100" /> },
);

// The /map view: the matching listings as a list (desktop) next to their pins. totalCount is how
// many matched in all — the API sends at most a capped number of pins, and the list says so.
// listHref: the same search as a list — where an empty map sends people, because a listing
// without coordinates yet is in the list but never on the map.
export function PropertyMapExplorer({ listings, totalCount, listHref }: { listings: Listing[]; totalCount: number; listHref: string }) {
  const t = useTranslations("MapPage");
  const tSearch = useTranslations("Search");
  const tType = useTranslations("PropertyType");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [overflowPoints, setOverflowPoints] = useState<MapPoint[] | null>(null);

  const points = useMemo<MapPoint[]>(
    () =>
      listings
        .filter((listing) => listing.property.location?.latitude != null && listing.property.location?.longitude != null)
        .map((listing) => ({ listing, lat: listing.property.location!.latitude!, lng: listing.property.location!.longitude! })),
    [listings],
  );

  const hasOverflow = overflowPoints != null && overflowPoints.length > 0;

  const listingList = (
    <>
      <div className="border-b border-ink-100 px-5 py-4">
        {/* The count is in the results toolbar above (visible on phones too, unlike this list). */}
        <h2 className="font-display text-lg font-medium text-ink-950">{t("title")}</h2>
        {totalCount > listings.length && (
          <p className="mt-2 rounded-lg bg-accent-50 px-3 py-2 text-xs text-accent-800">
            {tSearch("mapTruncated", { shown: listings.length, total: totalCount })}
          </p>
        )}
      </div>

      <ul className="divide-y divide-ink-100">
        {points.map(({ listing }) => (
          <li key={listing.id}>
            <button
              type="button"
              onClick={() => setSelectedId(listing.id)}
              className={cn(
                "flex w-full items-center gap-3 px-5 py-3 text-left transition-colors hover:bg-ink-50",
                selectedId === listing.id && "bg-brand-50",
              )}
            >
              <span className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-brand-800">
                {coverPhoto(listing) ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={coverPhoto(listing)!.url} alt="" className="h-full w-full object-cover" />
                ) : (
                  <PropertyIcon type={listing.property.propertyType} className="h-6 w-6 text-white/50" />
                )}
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-medium text-ink-900">{listing.title}</span>
                <span className="block text-xs text-ink-500">
                  {formatPrice(listing.price.amount, listing.price.currency)} · {tType(listing.property.propertyType)}
                </span>
              </span>
            </button>
          </li>
        ))}
      </ul>
    </>
  );

  return (
    <div>
      {/* Tall enough to fill the screen under the site header and the sticky filter bar.
          isolate: Leaflet's panes and controls use z-indexes up to 1000 — contained here, they
          can't paint over the sticky site header, the filter bar or its dropdowns. */}
      <div className="isolate flex h-[calc(100vh-13rem)] min-h-[520px] flex-col overflow-hidden rounded-3xl border border-ink-100 bg-white shadow-[var(--shadow-card)] lg:flex-row">
        {/* The permanent listings list is desktop-only — below lg there isn't room to show both
            it and the map usefully, so the map just takes the full view and listings surface
            through the cluster markers/overflow panel instead. */}
        <aside className="hidden lg:flex lg:h-full lg:w-96 lg:shrink-0 lg:flex-col lg:overflow-y-auto lg:border-r lg:border-ink-100 lg:bg-white">
          {listingList}
        </aside>

        {/* Cluster-overflow panel always overlays the map itself here (never the aside above),
            at every width — see ClusterOverflowPanel.tsx for its own responsive positioning. */}
        <div className="relative h-full flex-1">
          <PropertyMapFull
            points={points}
            selectedId={selectedId}
            onSelect={setSelectedId}
            cluster
            onClusterOverflow={setOverflowPoints}
            fitToPoints
          />
          {points.length === 0 && (
            <div className="pointer-events-none absolute inset-0 z-[1000] flex items-center justify-center p-6">
              <div className="pointer-events-auto max-w-sm rounded-2xl border border-line bg-white/95 px-6 py-5 text-center shadow-lg">
                <p className="font-semibold text-ink-900">{tSearch("mapEmptyTitle")}</p>
                <p className="mt-1 text-sm text-ink-500">{tSearch("mapEmptyHint")}</p>
                <Link href={listHref} className="mt-3 inline-block text-sm font-medium text-accent-600 hover:underline">
                  {tSearch("mapEmptyShowList")}
                </Link>
              </div>
            </div>
          )}
          {hasOverflow && (
            <>
              {/* Dimmed backdrop behind the bottom-sheet panel below lg, so it visibly reads as
                  a floating overlay above the map rather than plain stacked content — without
                  it, a full-coverage panel with no visible depth cue looked identical to inline
                  flow. Not needed at lg+, where the panel is a partial-width side panel with the
                  map and sidebar still visible. */}
              <div
                className="absolute inset-0 z-[999] bg-ink-950/40 lg:hidden"
                onClick={() => setOverflowPoints(null)}
              />
              <ClusterOverflowPanel points={overflowPoints} onClose={() => setOverflowPoints(null)} />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
