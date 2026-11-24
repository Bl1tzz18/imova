"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatRange } from "@/lib/search/applied";
import { activeFilterCount, clearAllFilters, detailFilterCount, singlePropertyType } from "@/lib/search/filters";
import { FilterPopover } from "./FilterPopover";
import { LocationFilter, PropertyTypePicker, RangeInputs, TransactionToggle, useLocationSummary } from "./FilterControls";
import { FiltersDrawer } from "./FiltersDrawer";
import { LocationSearchField } from "./LocationSearchField";
import { useSearchNavigation } from "./SearchNavigation";
import { ViewToggle } from "./ViewToggle";

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

function CountBadge({ count }: { count: number }) {
  return count > 0 ? <span className="rounded-full bg-accent-500 px-1.5 text-xs font-semibold text-white">{count}</span> : null;
}

// The pinned search filter bar (the /search list and the /map view share it) — only what narrows
// the search; how the results are shown (list/map, sorting) is the ResultsToolbar's, under it.
// Desktop: the filters people change most — transaction, type, location, price — each one click
// away, "Mai multe filtre" for the type's detailed filters (a side drawer) and reset. Phone: the
// location typeahead on top, then price, "Filtre" (a full-screen sheet with every filter) and the
// list/map switch (the toolbar row has only room for the count and sorting there).
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
        {/* Icon-only below xl (title + aria-label still name it), so the row fits at 1024px. */}
        <span className="hidden xl:inline">{t("resetShort")}</span>
      </button>
    ) : null;

  // The price pill: in the desktop bar and in the phone's second row (narrower there).
  const pricePopover = (panelClassName?: string, maxWidthClassName?: string) => (
    <FilterPopover
      label={t("pricePill")}
      value={price}
      onClear={() => change({ minPriceEur: null, maxPriceEur: null })}
      panelClassName={panelClassName}
      maxWidthClassName={maxWidthClassName}
    >
      {() => (
        <div>
          <span className="mb-2 block text-xs font-medium text-ink-600">{t("price")}</span>
          <RangeInputs minParam="minPriceEur" maxParam="maxPriceEur" label={t("price")} decimal />
        </div>
      )}
    </FilterPopover>
  );

  return (
    <>
      <div className="flex items-center gap-2">
        <div className="hidden flex-1 flex-wrap items-center gap-2 lg:flex">
          <TransactionToggle />
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
          {pricePopover()}
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

        {/* Phone: where (typeahead) on its own row, then price, every other filter and the list/map
            switch (wider screens have that one in the results toolbar under the bar). No reset
            button here — it doesn't fit beside a set price at 390px; the sheet's footer and the
            applied-filter chips under the bar clear filters. The price takes what's left and
            shortens its text. */}
        <div className="w-full space-y-2 lg:hidden">
          <LocationSearchField />
          <div className="flex items-center gap-2">
            <div className="min-w-0">{pricePopover("w-[min(340px,calc(100vw-2rem))]", "max-w-full")}</div>
            <button
              type="button"
              onClick={() => setDrawer("sheet")}
              className="flex h-10 shrink-0 items-center gap-2 rounded-full border border-line bg-white px-4 text-sm font-medium text-ink-800"
            >
              {SLIDERS_ICON}
              {t("filters")}
              <CountBadge count={filterCount} />
            </button>
            <div className="ml-auto shrink-0">
              <ViewToggle />
            </div>
          </div>
        </div>
      </div>

      <FiltersDrawer open={drawer !== null} variant={drawer ?? "side"} onClose={closeDrawer} totalCount={totalCount} />
    </>
  );
}
