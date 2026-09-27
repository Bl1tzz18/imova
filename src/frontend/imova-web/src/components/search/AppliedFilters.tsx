"use client";

import { useLocale, useTranslations } from "next-intl";
import { DETAIL_LAYOUTS } from "@/lib/property/detailLayouts";
import { appliedDetailFilters, formatRange, type AppliedFilter } from "@/lib/search/applied";
import { clearDetailFilters, singlePropertyType } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { useLocationSummary } from "./FilterControls";
import { useFilterOptions } from "./FilterOptions";
import { useSearchNavigation } from "./SearchNavigation";

type Chip = { id: string; remove: Record<string, string[] | null>; label: string; phoneOnly: boolean };

// One removable chip per applied drawer filter (see appliedDetailFilters), under the top bar. On a
// phone the bar has no location/price pills (and the page title only names the type and deal), so
// those two get chips there as well.
export function AppliedFilters() {
  const t = useTranslations("Search");
  const tAttr = useTranslations("Attributes");
  const tForm = useTranslations("PropertyForm");
  const tCondition = useTranslations("Condition");
  const tAmenity = useTranslations("Amenity");
  const tProximity = useTranslations("Proximity");
  const locale = useLocale();
  const { state, change } = useSearchNavigation();
  const { amenities, proximities } = useFilterOptions();
  const type = singlePropertyType(state);
  const location = useLocationSummary();
  const [minPrice, maxPrice] = [state.minPriceEur?.[0] ?? null, state.maxPriceEur?.[0] ?? null];

  const yesNo = (value: string) => (value === "true" ? tAttr("yes") : tAttr("no"));
  const number = (n: string) => Number(n).toLocaleString(locale);

  // The chip's text, or null while the name it needs (an amenity's) is still loading.
  function text(filter: AppliedFilter): string | null {
    switch (filter.kind) {
      case "amenity": {
        const amenity = amenities.find((a) => a.id === filter.value);
        return amenity ? (tAmenity.has(amenity.key) ? tAmenity(amenity.key) : amenity.labelRo) : null;
      }
      case "proximity": {
        const proximity = proximities.find((p) => p.id === filter.value);
        return proximity ? (tProximity.has(proximity.key) ? tProximity(proximity.key) : proximity.labelRo) : null;
      }
      case "range": {
        const label =
          filter.field === "area"
            ? type ? tForm(DETAIL_LAYOUTS[type].totalAreaLabel) : t("area")
            : filter.field === "yearBuilt" ? tForm("yearBuiltLabel") : tAttr(`${filter.field}.label`);
        return `${label}: ${formatRange(filter.min, filter.max, { from: t("min"), upTo: t("max") }, number)}`;
      }
      case "value":
        if (filter.filter?.kind === "choice") return `${tAttr(`${filter.field}.label`)}: ${tAttr(`${filter.field}.options.${filter.value}`)}`;
        if (filter.filter?.kind === "yesno") return `${tAttr(`${filter.field}.label`)}: ${yesNo(filter.value)}`;
        if (filter.field === "condition") return `${tForm("conditionLabel")}: ${tCondition(filter.value)}`;
        if (filter.field === "maxLeasePeriodMonths") return `${t("maxLeasePeriod")}: ${filter.value}`;
        return `${t(filter.field as "petsAllowed" | "utilitiesIncluded")}: ${yesNo(filter.value)}`;
    }
  }

  const chips = appliedDetailFilters(state).flatMap((filter): Chip[] => {
    const label = text(filter);
    return label === null ? [] : [{ id: filter.id, remove: filter.remove, label, phoneOnly: false }];
  });
  const phoneChips: Chip[] = [
    ...(location ? [{ id: "location", remove: { raionId: null, localitateId: null, chisinauSectorId: null }, label: location, phoneOnly: true }] : []),
    ...(minPrice !== null || maxPrice !== null
      ? [{ id: "price", remove: { minPriceEur: null, maxPriceEur: null }, label: `${t("pricePill")}: ${formatRange(minPrice, maxPrice, { from: t("min"), upTo: t("max") }, number)} €`, phoneOnly: true }]
      : []),
  ];
  if (chips.length + phoneChips.length === 0) return null;

  const clearAll = () => change(clearDetailFilters(state));

  return (
    <div className={cn("mt-3 flex flex-wrap items-center gap-2", chips.length === 0 && "lg:hidden")}>
      <ul className="contents" aria-label={t("appliedFilters")}>
        {[...phoneChips, ...chips].map(({ id, remove, label, phoneOnly }) => (
          <li key={id} className={cn(phoneOnly && "lg:hidden")}>
            <button
              type="button"
              onClick={() => change(remove)}
              aria-label={t("removeFilter", { label })}
              className="group flex h-8 items-center gap-1.5 rounded-full border border-brand-200 bg-brand-50 pl-3 pr-2 text-sm text-brand-800 hover:border-brand-300"
            >
              {label}
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" className="h-3.5 w-3.5 opacity-60 group-hover:opacity-100" aria-hidden>
                <path d="M6 6l12 12M18 6 6 18" strokeLinecap="round" />
              </svg>
            </button>
          </li>
        ))}
      </ul>
      {chips.length > 1 && (
        <button type="button" onClick={clearAll} className="px-1 text-sm font-medium text-accent-600 hover:underline">
          {t("clearDetails")}
        </button>
      )}
    </div>
  );
}
