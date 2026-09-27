"use client";

import { useCallback, useState, type ReactNode } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { formatRange } from "@/lib/search/applied";
import { activeFilterCount, clearAllFilters, detailFilterCount, singlePropertyType, switchViewHref, type SearchView } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { FilterPopover } from "./FilterPopover";
import { LocationFilter, PropertyTypePicker, RangeInputs, TransactionToggle, useLocationSummary } from "./FilterControls";
import { FiltersDrawer } from "./FiltersDrawer";
import { useSearchNavigation } from "./SearchNavigation";
import { SortSelect } from "./SortSelect";

const SLIDERS_ICON = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="h-4 w-4" aria-hidden>
    <path d="M4 7h11M4 17h7M18 7h2M14 17h6M15 4.5v5M11 14.5v5" />
  </svg>
);

const RESET_ICON = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
    <path d="M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5" />
  </svg>
);

const VIEW_ICONS: Record<SearchView, ReactNode> = {
  list: <path d="M8 6h13M8 12h13M8 18h13M3.5 6h.01M3.5 12h.01M3.5 18h.01" />,
  map: <path d="M9 4 3 6.5v13.5L9 17.5l6 2.5 6-2.5V4l-6 2.5L9 4ZM9 4v13.5M15 6.5V20" />,
};

// Listă | Hartă — the same search as cards or as pins; switching keeps every filter.
function ViewToggle() {
  const t = useTranslations("Search");
  const { state, view } = useSearchNavigation();
  return (
    <nav aria-label={t("viewLabel")} className="flex shrink-0 gap-1 rounded-full border border-line bg-white p-1">
      {(["list", "map"] as const).map((option) => (
        <Link
          key={option}
          href={switchViewHref(state, option)}
          aria-current={view === option ? "page" : undefined}
          className={cn(
            "flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium transition-colors",
            view === option ? "bg-ink-950 text-white" : "text-ink-600 hover:text-ink-950",
          )}
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
            {VIEW_ICONS[option]}
          </svg>
          <span className="hidden sm:inline">{t(option === "list" ? "listView" : "mapView")}</span>
        </Link>
      ))}
    </nav>
  );
}

function CountBadge({ count }: { count: number }) {
  return count > 0 ? <span className="rounded-full bg-accent-500 px-1.5 text-xs font-semibold text-white">{count}</span> : null;
}

// The search filter bar, above the results (the /search list and the /map view share it).
// Desktop: the filters people change most — transaction, type, location, price — each one click
// away, "Mai multe filtre" for the type's detailed filters (a side drawer), the list/map switch and
// sorting. Phone: "Filtre" (a full-screen sheet with every filter), reset and the switch.
export function SearchFilterBar({ totalCount }: { totalCount: number }) {
  const t = useTranslations("Search");
  const tType = useTranslations("PropertyType");
  const locale = useLocale();
  const { state, change } = useSearchNavigation();
  const [drawer, setDrawer] = useState<"side" | "sheet" | null>(null);
  const closeDrawer = useCallback(() => setDrawer(null), []);

  const propertyType = singlePropertyType(state);
  const location = useLocationSummary();
  const [minPrice, maxPrice] = [state.minPriceEur?.[0] ?? null, state.maxPriceEur?.[0] ?? null];
  const price =
    minPrice === null && maxPrice === null
      ? null
      : `${formatRange(minPrice, maxPrice, { from: t("min"), upTo: t("max") }, (n) => Number(n).toLocaleString(locale))} €`;
  const detailCount = detailFilterCount(state);
  const filterCount = activeFilterCount(state);
  // Every filter at once; shown only while there's something to reset.
  const reset =
    filterCount > 0 ? (
      <button
        type="button"
        onClick={() => change(clearAllFilters(state))}
        aria-label={t("reset")}
        title={t("reset")}
        className="flex h-10 shrink-0 items-center gap-1.5 rounded-full px-3 text-sm font-medium text-accent-600 hover:bg-accent-50"
      >
        {RESET_ICON}
        <span className="hidden lg:inline">{t("resetShort")}</span>
      </button>
    ) : null;

  return (
    <>
      <div className="flex items-center gap-2">
        <div className="hidden flex-1 flex-wrap items-center gap-2 lg:flex">
          <TransactionToggle className="w-[260px]" />
          <FilterPopover label={t("propertyType")} value={propertyType && tType(propertyType)} onClear={() => change({ propertyType: null })} panelClassName="w-[380px]">
            {(close) => <PropertyTypePicker idPrefix="bar" onPicked={close} />}
          </FilterPopover>
          <FilterPopover
            label={t("location")}
            value={location}
            onClear={() => change({ raionId: null, localitateId: null, chisinauSectorId: null })}
          >
            {() => <LocationFilter showClear={false} />}
          </FilterPopover>
          <FilterPopover label={t("pricePill")} value={price} onClear={() => change({ minPriceEur: null, maxPriceEur: null })}>
            {() => (
              <div>
                <span className="mb-2 block text-xs font-medium text-ink-600">{t("price")}</span>
                <RangeInputs minParam="minPriceEur" maxParam="maxPriceEur" label={t("price")} decimal />
              </div>
            )}
          </FilterPopover>
          <button
            type="button"
            onClick={() => setDrawer("side")}
            className="flex h-10 items-center gap-2 rounded-full border border-line bg-white px-4 text-sm font-medium text-ink-800 hover:border-ink-300"
          >
            {SLIDERS_ICON}
            {t("moreFilters")}
            <CountBadge count={detailCount} />
          </button>
          {reset}
        </div>

        <button
          type="button"
          onClick={() => setDrawer("sheet")}
          className="flex h-10 items-center gap-2 rounded-full border border-line bg-white px-4 text-sm font-medium text-ink-800 lg:hidden"
        >
          {SLIDERS_ICON}
          {t("filters")}
          <CountBadge count={filterCount} />
        </button>
        <div className="lg:hidden">{reset}</div>

        <div className="ml-auto flex shrink-0 items-center gap-2">
          <ViewToggle />
          {/* On a phone the row has no room for it: the list page shows sorting above the
              results instead (sorting is set once, it needn't stay pinned). */}
          <div className="hidden lg:block">
            <SortSelect />
          </div>
        </div>
      </div>

      <FiltersDrawer open={drawer !== null} variant={drawer ?? "side"} onClose={closeDrawer} totalCount={totalCount} />
    </>
  );
}
