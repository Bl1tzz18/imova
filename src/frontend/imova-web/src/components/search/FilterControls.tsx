"use client";

import { useTranslations } from "next-intl";
import { PropertyTypeCards } from "@/components/property/listing-form/PropertyTypeCards";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { PROPERTY_TYPES } from "@/lib/property/attributeSchema";
import { singlePropertyType } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { DebouncedNumberInput } from "./DebouncedNumberInput";
import { useFilterOptions } from "./FilterOptions";
import { useSearchNavigation } from "./SearchNavigation";

// The search controls the top bar and the filter drawer/sheet share, so each exists once.

export function RangeInputs({
  minParam,
  maxParam,
  label,
  allowNegative,
  decimal,
}: {
  minParam: string;
  maxParam: string;
  label: string;
  allowNegative?: boolean;
  decimal?: boolean;
}) {
  const t = useTranslations("Search");
  const { state, change } = useSearchNavigation();
  const one = (param: string) => state[param]?.[0] ?? "";
  return (
    <div className="grid grid-cols-2 gap-2">
      {[
        [minParam, t("min")],
        [maxParam, t("max")],
      ].map(([param, word]) => (
        <DebouncedNumberInput
          key={param}
          value={one(param)}
          onCommit={(v) => change({ [param]: v })}
          placeholder={word}
          ariaLabel={`${label} — ${word}`}
          allowNegative={allowNegative}
          decimal={decimal}
        />
      ))}
    </div>
  );
}

// Toate / Vânzare / Chirie.
export function TransactionToggle({ className }: { className?: string }) {
  const t = useTranslations("Search");
  const { state, change } = useSearchNavigation();
  const current = state.transactionType?.[0] ?? "";
  return (
    <div className={cn("flex gap-1 rounded-full border border-line bg-white p-1", className)} role="radiogroup" aria-label={t("transactionType")}>
      {[
        { value: "", label: t("all") },
        { value: "Sale", label: t("sale") },
        { value: "Rent", label: t("rent") },
      ].map((option) => (
        <button
          key={option.value || "all"}
          type="button"
          role="radio"
          aria-checked={current === option.value}
          onClick={() => change({ transactionType: option.value || null })}
          className={cn(
            "flex-1 whitespace-nowrap rounded-full px-3 py-1.5 text-center text-sm font-medium transition-colors",
            current === option.value ? "bg-ink-950 text-white" : "text-ink-600 hover:text-ink-950",
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}

// The listing form's type cards, one type or none ("Toate tipurile").
export function PropertyTypePicker({ idPrefix, onPicked }: { idPrefix: string; onPicked?: () => void }) {
  const t = useTranslations("Search");
  const tType = useTranslations("PropertyType");
  const { state, change } = useSearchNavigation();
  const propertyType = singlePropertyType(state);
  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <span className="text-xs font-medium text-ink-600">{t("propertyType")}</span>
        {propertyType && (
          <button type="button" onClick={() => change({ propertyType: null })} className="text-xs font-medium text-ink-500 hover:text-ink-900">
            {t("allTypes")}
          </button>
        )}
      </div>
      <PropertyTypeCards
        name={`${idPrefix}-propertyType`}
        value={propertyType ?? ""}
        onChange={(type) => {
          change({ propertyType: type });
          onPicked?.();
        }}
        options={PROPERTY_TYPES.map((type) => ({ value: type, label: tType(type) }))}
        label={t("propertyType")}
        required={false}
        compact
      />
      {!propertyType && <p className="mt-2 text-xs text-ink-500">{t("chooseTypeHint")}</p>}
    </div>
  );
}

// Raion, then a Chișinău sector or a localitate. showClear: false where the container has its own
// clear action (the top bar's panel).
export function LocationFilter({ showClear = true }: { showClear?: boolean }) {
  const t = useTranslations("Search");
  const { state, change } = useSearchNavigation();
  const { raioane, sectors, localitati } = useFilterOptions();
  const raionId = state.raionId?.[0] ?? "";
  const isChisinau = raioane.find((r) => r.id === raionId)?.localityLabel === "Sector";
  return (
    <div className="space-y-2">
      <SearchableSelect
        name="raionId"
        value={raionId}
        onChange={(id) => change({ raionId: id || null, localitateId: null, chisinauSectorId: null })}
        options={raioane.map((r) => ({ id: r.id, label: r.nameRo }))}
        placeholder={t("anyRaion")}
        searchPlaceholder={t("searchRaion")}
        noResultsText={t("noLocation")}
      />
      {raionId && isChisinau && (
        <SearchableSelect
          name="chisinauSectorId"
          value={state.chisinauSectorId?.[0] ?? ""}
          onChange={(id) => change({ chisinauSectorId: id || null, localitateId: null })}
          options={sectors.map((s) => ({ id: s.id, label: s.name }))}
          placeholder={t("anySector")}
          searchPlaceholder={t("searchSector")}
          noResultsText={t("noLocation")}
        />
      )}
      {raionId && (
        <SearchableSelect
          name="localitateId"
          value={state.localitateId?.[0] ?? ""}
          onChange={(id) => change({ localitateId: id || null, chisinauSectorId: null })}
          options={localitati.map((l) => ({ id: l.id, label: l.nameRo }))}
          placeholder={isChisinau ? t("anySuburb") : t("anyLocalitate")}
          searchPlaceholder={t("searchLocalitate")}
          noResultsText={t("noLocation")}
        />
      )}
      {raionId && showClear && (
        <button type="button" onClick={() => change({ raionId: null, localitateId: null, chisinauSectorId: null })} className="text-xs font-medium text-ink-500 hover:text-ink-900">
          {t("clearLocation")}
        </button>
      )}
    </div>
  );
}

// "Botanica, Chișinău" / "Ialoveni" — the location pill's text, or null with none picked.
export function useLocationSummary(): string | null {
  const { state } = useSearchNavigation();
  const { raioane, sectors, localitati } = useFilterOptions();
  const raion = raioane.find((r) => r.id === state.raionId?.[0]);
  if (!raion) return null;
  const place =
    sectors.find((s) => s.id === state.chisinauSectorId?.[0])?.name ?? localitati.find((l) => l.id === state.localitateId?.[0])?.nameRo;
  return place ? `${place}, ${raion.nameRo}` : raion.nameRo;
}
