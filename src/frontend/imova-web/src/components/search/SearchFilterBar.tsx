"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatRange } from "@/lib/search/applied";
import { activeFilterCount, detailFilterCount, singlePropertyType } from "@/lib/search/filters";
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

function CountBadge({ count }: { count: number }) {
  return count > 0 ? <span className="rounded-full bg-accent-500 px-1.5 text-xs font-semibold text-white">{count}</span> : null;
}

// The search page's filter bar, above the results. Desktop: the filters people change most —
// transaction, type, location, price — each one click away, "Mai multe filtre" for the type's
// detailed filters (a side drawer), and sorting. Phone: "Filtre" (a full-screen sheet with every
// filter) and sorting.
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
        </div>

        <button
          type="button"
          onClick={() => setDrawer("sheet")}
          className="flex h-10 items-center gap-2 rounded-full border border-line bg-white px-4 text-sm font-medium text-ink-800 lg:hidden"
        >
          {SLIDERS_ICON}
          {t("filters")}
          <CountBadge count={activeFilterCount(state)} />
        </button>

        <div className="ml-auto shrink-0">
          <SortSelect />
        </div>
      </div>

      <FiltersDrawer open={drawer !== null} variant={drawer ?? "side"} onClose={closeDrawer} totalCount={totalCount} />
    </>
  );
}
