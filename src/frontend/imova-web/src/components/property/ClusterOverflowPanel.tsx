"use client";

import { useTranslations } from "next-intl";
import { PropertyCard } from "@/components/property/PropertyCard";
import type { MapPoint } from "@/components/property/PropertyMapFull";

function CloseIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className={className}>
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  );
}

// Shown when a cluster is clicked at the map's max zoom and still can't break apart further
// (its listings share the same or a near-identical coordinate — see PropertyMapFull.tsx's
// ClusteredPropertyMarkers). Always overlays the map itself (absolutely positioned within the
// map's relative wrapper in PropertyMapExplorer.tsx), never the permanent listings sidebar next
// to it, at any width — a bottom sheet over the map below `lg` (matching /map's own breakpoint
// for switching between a stacked and a side-by-side layout), the original right-edge panel at
// `lg+` where there's room for it beside the sidebar. Reuses PropertyCard as-is (its
// showFloor/hidePerMonthSuffix props exist for exactly this panel) rather than building
// separate card markup, so these stay visually consistent with search/saved-listings.
export function ClusterOverflowPanel({ points, onClose }: { points: MapPoint[]; onClose: () => void }) {
  const t = useTranslations("MapPage");

  return (
    <div className="absolute left-0 right-0 bottom-0 z-[1000] flex max-h-[85%] flex-col rounded-t-2xl border-t border-ink-100 bg-white shadow-[var(--shadow-card-hover)] lg:left-auto lg:top-0 lg:right-0 lg:bottom-0 lg:w-full lg:max-w-sm lg:max-h-none lg:rounded-t-none lg:rounded-l-2xl lg:border-l lg:border-t-0">
      <div className="flex shrink-0 items-center justify-between border-b border-ink-100 px-4 py-3">
        <h2 className="text-sm font-semibold text-ink-950">{t("pointListingsCount", { count: points.length })}</h2>
        <button
          type="button"
          onClick={onClose}
          aria-label={t("closePanel")}
          title={t("closePanel")}
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-ink-500 transition-colors hover:bg-ink-100"
        >
          <CloseIcon className="h-4 w-4" />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-3">
        <div className="flex flex-col gap-3">
          {points.map((point) => (
            <PropertyCard key={point.listing.id} listing={point.listing} showFloor hidePerMonthSuffix />
          ))}
        </div>
      </div>
    </div>
  );
}
