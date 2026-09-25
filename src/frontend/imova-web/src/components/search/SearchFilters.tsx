"use client";

import { useEffect, useState, type ReactNode } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { SelectInput } from "@/components/ui/Field";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { getAmenities } from "@/lib/api/amenities";
import { getChisinauSectors, getLocalitati, getRaioane, type ChisinauSector, type Localitate, type Raion } from "@/lib/api/locations";
import { getProximities } from "@/lib/api/proximities";
import { ATTRIBUTE_SCHEMA, PROPERTY_TYPES } from "@/lib/property/attributeSchema";
import { applicableTypeFilters, rentalFiltersApply, SEARCH_PATH, singlePropertyType } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import type { Amenity, Proximity } from "@/types/listing";
import { DebouncedNumberInput } from "./DebouncedNumberInput";
import { useSearchNavigation } from "./SearchNavigation";

function Section({ title, children, collapsible = false, defaultOpen = true }: { title: string; children: ReactNode; collapsible?: boolean; defaultOpen?: boolean }) {
  if (collapsible) {
    return (
      <details open={defaultOpen} className="group border-t border-line py-4">
        <summary className="flex cursor-pointer list-none items-center justify-between text-sm font-semibold text-ink-950">
          {title}
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4 text-ink-400 transition-transform group-open:rotate-180" aria-hidden>
            <path d="m6 9 6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </summary>
        <div className="mt-3">{children}</div>
      </details>
    );
  }
  return (
    <section className="border-t border-line py-4 first:border-t-0 first:pt-0">
      <h3 className="mb-3 text-sm font-semibold text-ink-950">{title}</h3>
      {children}
    </section>
  );
}

// The /cauta filter panel — a sidebar on desktop, inside a full-screen sheet on mobile. Every change
// goes straight to the URL (see SearchNavigation); number fields wait for typing to pause.
// inSheet: the mobile sheet has its own "Filtre" title, so the panel doesn't repeat it.
export function SearchFilters({ inSheet = false }: { inSheet?: boolean }) {
  const t = useTranslations("Search");
  const tType = useTranslations("PropertyType");
  const tAttr = useTranslations("Attributes");
  const tAmenity = useTranslations("Amenity");
  const tProximity = useTranslations("Proximity");
  const { state, change, pending } = useSearchNavigation();

  const [raioane, setRaioane] = useState<Raion[]>([]);
  const [localitati, setLocalitati] = useState<Localitate[]>([]);
  const [sectors, setSectors] = useState<ChisinauSector[]>([]);
  const [amenities, setAmenities] = useState<Amenity[]>([]);
  const [proximities, setProximities] = useState<Proximity[]>([]);

  const one = (param: string) => state[param]?.[0] ?? "";
  const many = (param: string) => state[param] ?? [];
  const raionId = one("raionId");
  const selectedRaion = raioane.find((r) => r.id === raionId);
  const isChisinau = selectedRaion?.localityLabel === "Sector";
  const selectedTypes = many("propertyType");
  const singleType = singlePropertyType(state);

  useEffect(() => {
    getRaioane().then(setRaioane).catch(() => setRaioane([]));
    getChisinauSectors().then(setSectors).catch(() => setSectors([]));
    getAmenities().then(setAmenities).catch(() => setAmenities([]));
    getProximities().then(setProximities).catch(() => setProximities([]));
  }, []);

  useEffect(() => {
    if (!raionId) {
      setLocalitati([]);
      return;
    }
    getLocalitati(raionId).then(setLocalitati).catch(() => setLocalitati([]));
  }, [raionId]);

  function toggle(param: string, value: string, on: boolean) {
    const current = many(param);
    change({ [param]: on ? [...current, value] : current.filter((v) => v !== value) });
  }

  function range(minParam: string, maxParam: string, label: string, allowNegative = false) {
    return (
      <div className="grid grid-cols-2 gap-2">
        <DebouncedNumberInput value={one(minParam)} onCommit={(v) => change({ [minParam]: v })} placeholder={t("min")} ariaLabel={`${label} — ${t("min")}`} allowNegative={allowNegative} />
        <DebouncedNumberInput value={one(maxParam)} onCommit={(v) => change({ [maxParam]: v })} placeholder={t("max")} ariaLabel={`${label} — ${t("max")}`} allowNegative={allowNegative} />
      </div>
    );
  }

  function yesNoAny(param: string, label: string) {
    return (
      <label className="block">
        <span className="mb-1 block text-xs font-medium text-ink-600">{label}</span>
        <SelectInput value={one(param)} onChange={(e) => change({ [param]: e.target.value || null })} className="h-10 text-sm">
          <option value="">{t("any")}</option>
          <option value="true">{tAttr("yes")}</option>
          <option value="false">{tAttr("no")}</option>
        </SelectInput>
      </label>
    );
  }

  // Amenities that apply to at least one selected type (all of them while no type is picked).
  const offeredAmenities = amenities.filter(
    (a) => selectedTypes.length === 0 || selectedTypes.some((type) => a.applicablePropertyTypes.includes(type)),
  );
  const segment = (active: boolean) =>
    cn("flex-1 rounded-full px-3 py-1.5 text-center text-sm font-medium transition-colors", active ? "bg-ink-950 text-white" : "text-ink-600 hover:text-ink-950");

  return (
    <div className="text-ink-800">
      <div className={cn("mb-4 flex items-center gap-3", inSheet ? "justify-end" : "justify-between")}>
        {!inSheet && <h2 className="font-hero text-lg font-bold text-ink-950">{t("filters")}</h2>}
        <div className="flex items-center gap-3">
          {pending && <span className="text-xs text-ink-400">{t("updating")}</span>}
          <Link href={SEARCH_PATH} className="text-sm font-medium text-accent-600 hover:underline">
            {t("reset")}
          </Link>
        </div>
      </div>

      <Section title={t("transactionType")}>
        <div className="flex gap-1 rounded-full border border-line bg-white p-1" role="radiogroup" aria-label={t("transactionType")}>
          {[
            { value: "", label: t("all") },
            { value: "Sale", label: t("sale") },
            { value: "Rent", label: t("rent") },
          ].map((option) => (
            <button
              key={option.value || "all"}
              type="button"
              role="radio"
              aria-checked={one("transactionType") === option.value}
              onClick={() => change({ transactionType: option.value || null })}
              className={segment(one("transactionType") === option.value)}
            >
              {option.label}
            </button>
          ))}
        </div>
      </Section>

      <Section title={t("propertyType")}>
        <div className="grid grid-cols-2 gap-x-3 gap-y-2">
          {PROPERTY_TYPES.map((type) => (
            <Checkbox key={type} checked={selectedTypes.includes(type)} onChange={(e) => toggle("propertyType", type, e.target.checked)} className="text-ink-700">
              {tType(type)}
            </Checkbox>
          ))}
        </div>
        {selectedTypes.length > 1 && <p className="mt-2 text-xs text-ink-500">{t("multipleTypesHint")}</p>}
      </Section>

      <Section title={t("price")}>{range("minPriceEur", "maxPriceEur", t("price"))}</Section>

      <Section title={t("location")}>
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
              value={one("chisinauSectorId")}
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
              value={one("localitateId")}
              onChange={(id) => change({ localitateId: id || null, chisinauSectorId: null })}
              options={localitati.map((l) => ({ id: l.id, label: l.nameRo }))}
              placeholder={isChisinau ? t("anySuburb") : t("anyLocalitate")}
              searchPlaceholder={t("searchLocalitate")}
              noResultsText={t("noLocation")}
            />
          )}
          {raionId && (
            <button type="button" onClick={() => change({ raionId: null, localitateId: null, chisinauSectorId: null })} className="text-xs font-medium text-ink-500 hover:text-ink-900">
              {t("clearLocation")}
            </button>
          )}
        </div>
      </Section>

      <Section title={t("area")}>{range("minAreaM2", "maxAreaM2", t("area"))}</Section>

      {singleType && applicableTypeFilters(state).length > 0 && (
        <Section title={t("typeDetails", { type: tType(singleType) })}>
          <div className="space-y-3">
            {applicableTypeFilters(state).map((filter) => {
              const label = tAttr(`${filter.field}.label`);
              if (filter.kind === "enum") {
                const schemaField = ATTRIBUTE_SCHEMA[singleType].find((f) => f.name === filter.field);
                const options = schemaField && "options" in schemaField ? schemaField.options : [];
                return (
                  <label key={filter.field} className="block">
                    <span className="mb-1 block text-xs font-medium text-ink-600">{label}</span>
                    <SelectInput value={one(filter.params[0])} onChange={(e) => change({ [filter.params[0]]: e.target.value || null })} className="h-10 text-sm">
                      <option value="">{t("any")}</option>
                      {options.map((option) => (
                        <option key={option} value={option}>
                          {tAttr(`${filter.field}.options.${option}`)}
                        </option>
                      ))}
                    </SelectInput>
                  </label>
                );
              }
              return (
                <div key={filter.field}>
                  <span className="mb-1 block text-xs font-medium text-ink-600">{label}</span>
                  {filter.kind === "range" ? (
                    range(filter.params[0], filter.params[1], label, filter.field === "floor")
                  ) : (
                    <DebouncedNumberInput value={one(filter.params[0])} onCommit={(v) => change({ [filter.params[0]]: v })} placeholder={t("atLeast")} ariaLabel={`${label} — ${t("atLeast")}`} />
                  )}
                </div>
              );
            })}
          </div>
        </Section>
      )}

      {rentalFiltersApply(state) && (
        <Section title={t("rentalTerms")}>
          <div className="space-y-3">
            {yesNoAny("petsAllowed", t("petsAllowed"))}
            {yesNoAny("utilitiesIncluded", t("utilitiesIncluded"))}
            <div>
              <span className="mb-1 block text-xs font-medium text-ink-600">{t("maxLeasePeriod")}</span>
              <DebouncedNumberInput value={one("maxLeasePeriodMonths")} onCommit={(v) => change({ maxLeasePeriodMonths: v })} placeholder={t("months")} ariaLabel={t("maxLeasePeriod")} />
            </div>
          </div>
        </Section>
      )}

      <Section title={t("amenities", { count: many("amenityIds").length })} collapsible defaultOpen={many("amenityIds").length > 0}>
        <div className="grid grid-cols-1 gap-y-2">
          {offeredAmenities.map((a) => (
            <Checkbox key={a.id} checked={many("amenityIds").includes(a.id)} onChange={(e) => toggle("amenityIds", a.id, e.target.checked)} className="text-ink-700">
              {tAmenity.has(a.key) ? tAmenity(a.key) : a.labelRo}
            </Checkbox>
          ))}
        </div>
      </Section>

      <Section title={t("proximities", { count: many("proximityIds").length })} collapsible defaultOpen={many("proximityIds").length > 0}>
        <div className="grid grid-cols-1 gap-y-2">
          {proximities.map((p) => (
            <Checkbox key={p.id} checked={many("proximityIds").includes(p.id)} onChange={(e) => toggle("proximityIds", p.id, e.target.checked)} className="text-ink-700">
              {tProximity.has(p.key) ? tProximity(p.key) : p.labelRo}
            </Checkbox>
          ))}
        </div>
      </Section>
    </div>
  );
}
