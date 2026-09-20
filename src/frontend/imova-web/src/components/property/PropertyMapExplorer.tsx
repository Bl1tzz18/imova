"use client";

import dynamic from "next/dynamic";
import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ClusterOverflowPanel } from "@/components/property/ClusterOverflowPanel";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { cn } from "@/lib/utils/cn";
import { formatPrice } from "@/lib/utils/format";
import type { Property } from "@/types/property";
import type { MapPoint } from "@/components/property/PropertyMapFull";

const PropertyMapFull = dynamic(
  () => import("@/components/property/PropertyMapFull").then((m) => m.PropertyMapFull),
  { ssr: false, loading: () => <div className="h-full w-full animate-pulse bg-ink-100" /> },
);

export function PropertyMapExplorer({ properties }: { properties: Property[] }) {
  const t = useTranslations("MapPage");
  const tType = useTranslations("PropertyType");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [overflowPoints, setOverflowPoints] = useState<MapPoint[] | null>(null);

  const points = useMemo<MapPoint[]>(
    () =>
      properties
        .filter((property) => property.location?.latitude != null && property.location?.longitude != null)
        .map((property) => ({ property, lat: property.location!.latitude!, lng: property.location!.longitude! })),
    [properties],
  );

  const hasOverflow = overflowPoints != null && overflowPoints.length > 0;

  const listingList = (
    <>
      <div className="border-b border-ink-100 px-5 py-4">
        <h1 className="font-display text-lg font-medium text-ink-950">{t("title")}</h1>
        <p className="mt-0.5 text-sm text-ink-500">
          {points.length > 0 ? t("resultsCount", { count: points.length }) : t("noResults")}
        </p>
      </div>

      <ul className="divide-y divide-ink-100">
        {points.map(({ property }) => (
          <li key={property.id}>
            <button
              type="button"
              onClick={() => setSelectedId(property.id)}
              className={cn(
                "flex w-full items-center gap-3 px-5 py-3 text-left transition-colors hover:bg-ink-50",
                selectedId === property.id && "bg-brand-50",
              )}
            >
              <span className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-brand-800">
                {property.media[0] ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={property.media[0].url} alt="" className="h-full w-full object-cover" />
                ) : (
                  <PropertyIcon type={property.propertyType} className="h-6 w-6 text-white/50" />
                )}
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-medium text-ink-900">{property.title}</span>
                <span className="block text-xs text-ink-500">
                  {formatPrice(property.price, property.currency)} · {tType(property.propertyType)}
                </span>
              </span>
            </button>
          </li>
        ))}
      </ul>
    </>
  );

  return (
    <div className="mx-auto max-w-[100rem] px-4 py-6 sm:px-6 sm:py-8">
      <div className="flex h-[88vh] min-h-[680px] flex-col overflow-hidden rounded-3xl border border-ink-100 bg-white shadow-[var(--shadow-card)] lg:flex-row">
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
          />
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
