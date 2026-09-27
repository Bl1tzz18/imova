"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { SECTION_ICONS } from "@/components/property/sectionIcons";
import { AccordionSection } from "@/components/ui/AccordionSection";
import { Checkbox } from "@/components/ui/Checkbox";
import { SelectInput } from "@/components/ui/Field";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { getAmenities } from "@/lib/api/amenities";
import { getChisinauSectors, getLocalitati, getRaioane, type ChisinauSector, type Localitate, type Raion } from "@/lib/api/locations";
import { getProximities } from "@/lib/api/proximities";
import { ATTRIBUTE_SCHEMA, PROPERTY_TYPES } from "@/lib/property/attributeSchema";
import { filterSections, type FilterSection, type FilterSectionId } from "@/lib/search/filterSections";
import { SEARCH_PATH, singlePropertyType, type TypeSpecificFilter } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import type { Amenity, Proximity } from "@/types/listing";
import { DebouncedNumberInput } from "./DebouncedNumberInput";
import { useSearchNavigation } from "./SearchNavigation";

const ICONS: Record<FilterSectionId, ReactNode> = {
  ...SECTION_ICONS,
  basics: <path d="M4 7h11M4 17h7M18 7h2M14 17h6M15 4.5v5M11 14.5v5" />,
  location: <path d="M9 4 3 6.5v13.5L9 17.5l6 2.5 6-2.5V4l-6 2.5L9 4ZM9 4v13.5M15 6.5V20" />,
  price: <path d="M3.5 12.2V4.5a1 1 0 0 1 1-1h7.7l8.3 8.3a1.4 1.4 0 0 1 0 2l-6.7 6.7a1.4 1.4 0 0 1-2 0L3.5 12.2ZM8 8h.01" />,
  area: SECTION_ICONS.areas,
  rentalTerms: <path d="M7 3h7l4 4v14H7V3ZM14 3v4h4M10 12h5M10 16h5" />,
};

// Sections that have their own Search title; the rest are named like the listing form's.
const SEARCH_TITLES: Partial<Record<FilterSectionId, string>> = {
  basics: "basics",
  location: "location",
  price: "price",
  area: "area",
  rentalTerms: "rentalTerms",
};

// The /search filter panel — a sidebar on desktop, inside a full-screen sheet on mobile, split into
// collapsible sections (see filterSections). Every change goes straight to the URL (see
// SearchNavigation); number fields wait for typing to pause.
// inSheet: the mobile sheet has its own "Filtre" title, so the panel doesn't repeat it.
export function SearchFilters({ inSheet = false }: { inSheet?: boolean }) {
  const t = useTranslations("Search");
  const tType = useTranslations("PropertyType");
  const tAttr = useTranslations("Attributes");
  const tAmenity = useTranslations("Amenity");
  const tProximity = useTranslations("Proximity");
  const tSections = useTranslations("PropertyForm.detailSections");
  const { state, change, pending } = useSearchNavigation();

  const [raioane, setRaioane] = useState<Raion[]>([]);
  const [localitati, setLocalitati] = useState<Localitate[]>([]);
  const [sectors, setSectors] = useState<ChisinauSector[]>([]);
  const [amenities, setAmenities] = useState<Amenity[]>([]);
  const [proximities, setProximities] = useState<Proximity[]>([]);
  // Sections the user opened or collapsed; the rest follow FilterSection.openByDefault.
  const [toggled, setToggled] = useState<Partial<Record<FilterSectionId, boolean>>>({});

  const one = (param: string) => state[param]?.[0] ?? "";
  const many = (param: string) => state[param] ?? [];
  const raionId = one("raionId");
  const selectedRaion = raioane.find((r) => r.id === raionId);
  const isChisinau = selectedRaion?.localityLabel === "Sector";
  const selectedTypes = many("propertyType");
  const singleType = singlePropertyType(state);
  const sections = useMemo(() => filterSections(state, amenities, proximities), [state, amenities, proximities]);

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

  // A section with an active filter opens (on load, or once its list arrives) and then stays open
  // until collapsed by hand — clearing its last filter doesn't snap it shut under the cursor.
  useEffect(() => {
    setToggled((prev) => {
      const opened = sections.filter((s) => s.activeCount > 0 && !(s.id in prev));
      return opened.length === 0 ? prev : { ...prev, ...Object.fromEntries(opened.map((s) => [s.id, true])) };
    });
  }, [sections]);

  const isOpen = (s: FilterSection) => toggled[s.id] ?? (s.openByDefault || s.activeCount > 0);

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


  function typeFilter(filter: TypeSpecificFilter) {
    const label = tAttr(`${filter.field}.label`);
    if (filter.kind === "enum") {
      const schemaField = singleType ? ATTRIBUTE_SCHEMA[singleType].find((f) => f.name === filter.field) : undefined;
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
  }

  const segment = (active: boolean) =>
    cn("flex-1 rounded-full px-3 py-1.5 text-center text-sm font-medium transition-colors", active ? "bg-ink-950 text-white" : "text-ink-600 hover:text-ink-950");

  function sectionBody(section: FilterSection) {
    switch (section.id) {
      case "basics":
        return (
          <div className="space-y-4">
            <div>
              <span className="mb-2 block text-xs font-medium text-ink-600">{t("transactionType")}</span>
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
            </div>
            <div>
              <span className="mb-2 block text-xs font-medium text-ink-600">{t("propertyType")}</span>
              <div className="grid grid-cols-2 gap-x-3 gap-y-2">
                {PROPERTY_TYPES.map((type) => (
                  <Checkbox key={type} checked={selectedTypes.includes(type)} onChange={(e) => toggle("propertyType", type, e.target.checked)} className="text-ink-700">
                    {tType(type)}
                  </Checkbox>
                ))}
              </div>
              {selectedTypes.length !== 1 && <p className="mt-2 text-xs text-ink-500">{t("multipleTypesHint")}</p>}
            </div>
          </div>
        );
      case "location":
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
        );
      case "price":
        return range("minPriceEur", "maxPriceEur", t("price"));
      case "rentalTerms":
        return (
          <div className="space-y-3">
            {yesNoAny("petsAllowed", t("petsAllowed"))}
            {yesNoAny("utilitiesIncluded", t("utilitiesIncluded"))}
            <div>
              <span className="mb-1 block text-xs font-medium text-ink-600">{t("maxLeasePeriod")}</span>
              <DebouncedNumberInput value={one("maxLeasePeriodMonths")} onCommit={(v) => change({ maxLeasePeriodMonths: v })} placeholder={t("months")} ariaLabel={t("maxLeasePeriod")} />
            </div>
          </div>
        );
      case "proximities":
        return (
          <div className="grid grid-cols-1 gap-y-2">
            {proximities.map((p) => (
              <Checkbox key={p.id} checked={many("proximityIds").includes(p.id)} onChange={(e) => toggle("proximityIds", p.id, e.target.checked)} className="text-ink-700">
                {tProximity.has(p.key) ? tProximity(p.key) : p.labelRo}
              </Checkbox>
            ))}
          </div>
        );
    }
    if (section.amenities.length > 0) {
      return (
        <div className="grid grid-cols-1 gap-y-2">
          {section.amenities.map((a) => (
            <Checkbox key={a.id} checked={many("amenityIds").includes(a.id)} onChange={(e) => toggle("amenityIds", a.id, e.target.checked)} className="text-ink-700">
              {tAmenity.has(a.key) ? tAmenity(a.key) : a.labelRo}
            </Checkbox>
          ))}
        </div>
      );
    }
    return (
      <div className="space-y-3">
        {section.area && (
          <div>
            {section.typeFilters.length > 0 && <span className="mb-1 block text-xs font-medium text-ink-600">{t("area")}</span>}
            {range("minAreaM2", "maxAreaM2", t("area"))}
          </div>
        )}
        {section.typeFilters.map(typeFilter)}
      </div>
    );
  }

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

      <div className="space-y-2.5">
        {sections.map((section) => {
          const titleKey = SEARCH_TITLES[section.id];
          return (
            <AccordionSection
              key={section.id}
              id={`${inSheet ? "sheet" : "filters"}-${section.id}`}
              icon={ICONS[section.id]}
              title={titleKey ? t(titleKey) : tSections(section.id)}
              status={
                section.activeCount > 0 && (
                  <span className="rounded-full bg-accent-500 px-1.5 text-xs font-semibold text-white" aria-label={t("activeFilters", { count: section.activeCount })}>
                    {section.activeCount}
                  </span>
                )
              }
              open={isOpen(section)}
              onToggle={() => setToggled((prev) => ({ ...prev, [section.id]: !isOpen(section) }))}
            >
              {sectionBody(section)}
            </AccordionSection>
          );
        })}
      </div>
    </div>
  );
}
