"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { SECTION_ICONS } from "@/components/property/sectionIcons";
import { AccordionSection } from "@/components/ui/AccordionSection";
import { Checkbox } from "@/components/ui/Checkbox";
import { SelectInput } from "@/components/ui/Field";
import { GENERAL_CONDITIONS } from "@/lib/property/attributeSchema";
import { DETAIL_LAYOUTS } from "@/lib/property/detailLayouts";
import { filterSections, type FilterItem, type FilterSection, type FilterSectionId } from "@/lib/search/filterSections";
import { CONDITION_PARAM, petsFilterApplies, singlePropertyType, type AttributeFilter } from "@/lib/search/filters";
import { DebouncedNumberInput } from "./DebouncedNumberInput";
import { LocationFilter, PropertyTypePicker, RangeInputs, TransactionToggle } from "./FilterControls";
import { useFilterOptions } from "./FilterOptions";
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

// Sections the top bar already covers (transaction & type, location, price).
const BAR_SECTIONS: readonly FilterSectionId[] = ["basics", "location", "price"];

// The search filters as collapsible sections (see filterSections): once a property type is picked,
// the same sections and fields as that type's listing form. Every change goes straight to the URL
// (see SearchNavigation); number fields wait for typing to pause.
// scope "details": the desktop "Mai multe filtre" drawer, next to the top bar — so without the
// bar's own sections. scope "all": the mobile sheet, which is the only filter UI on a phone.
export function SearchFilters({ scope, idPrefix }: { scope: "all" | "details"; idPrefix: string }) {
  const t = useTranslations("Search");
  const tAttr = useTranslations("Attributes");
  const tAmenity = useTranslations("Amenity");
  const tProximity = useTranslations("Proximity");
  const tForm = useTranslations("PropertyForm");
  const tCondition = useTranslations("Condition");
  const { state, change } = useSearchNavigation();
  const { amenities, proximities } = useFilterOptions();
  // Sections the user opened or collapsed; the rest follow FilterSection.openByDefault.
  const [toggled, setToggled] = useState<Partial<Record<FilterSectionId, boolean>>>({});

  const one = (param: string) => state[param]?.[0] ?? "";
  const many = (param: string) => state[param] ?? [];
  const propertyType = singlePropertyType(state);
  const sections = useMemo(
    () => filterSections(state, amenities, proximities).filter((s) => scope === "all" || !BAR_SECTIONS.includes(s.id)),
    [state, amenities, proximities, scope],
  );

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

  function range(minParam: string, maxParam: string, label: string, options: { allowNegative?: boolean; decimal?: boolean } = {}) {
    return <RangeInputs minParam={minParam} maxParam={maxParam} label={label} {...options} />;
  }

  // "Any" or one value — a choice or a yes/no filter.
  function select(param: string, label: string, options: { value: string; label: string }[]) {
    return (
      <label key={param} className="block">
        <span className="mb-1 block text-xs font-medium text-ink-600">{label}</span>
        <SelectInput value={one(param)} onChange={(e) => change({ [param]: e.target.value || null })} className="h-10 text-sm">
          <option value="">{t("any")}</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </SelectInput>
      </label>
    );
  }

  const yesNo = [
    { value: "true", label: tAttr("yes") },
    { value: "false", label: tAttr("no") },
  ];

  function labelled(key: string, label: string, control: ReactNode) {
    return (
      <div key={key}>
        <span className="mb-1 block text-xs font-medium text-ink-600">{label}</span>
        {control}
      </div>
    );
  }

  function attributeFilter(filter: AttributeFilter) {
    const label = tAttr(`${filter.field}.label`);
    const schema = filter.schema;
    if (filter.kind === "choice" && "options" in schema) {
      return select(filter.params[0], label, schema.options.map((o) => ({ value: o, label: tAttr(`${filter.field}.options.${o}`) })));
    }
    if (filter.kind === "yesno") return select(filter.params[0], label, yesNo);
    const allowNegative = "min" in schema && schema.min < 0;
    return labelled(filter.field, label, range(filter.params[0], filter.params[1], label, { allowNegative, decimal: schema.kind === "decimal" }));
  }

  function item(entry: FilterItem, withLabel: boolean) {
    switch (entry.kind) {
      case "area": {
        const label = propertyType ? tForm(DETAIL_LAYOUTS[propertyType].totalAreaLabel) : t("area");
        const control = range("minAreaM2", "maxAreaM2", label, { decimal: true });
        return withLabel ? labelled("area", label, control) : <div key="area">{control}</div>;
      }
      case "yearBuilt":
        return labelled("yearBuilt", tForm("yearBuiltLabel"), range("minYearBuilt", "maxYearBuilt", tForm("yearBuiltLabel")));
      case "condition":
        return select(CONDITION_PARAM, tForm("conditionLabel"), GENERAL_CONDITIONS.map((c) => ({ value: c, label: tCondition(c) })));
      case "attribute":
        return attributeFilter(entry.filter);
    }
  }

  function sectionBody(section: FilterSection) {
    switch (section.id) {
      case "basics":
        return (
          <div className="space-y-4">
            <div>
              <span className="mb-2 block text-xs font-medium text-ink-600">{t("transactionType")}</span>
              <TransactionToggle />
            </div>
            <PropertyTypePicker idPrefix={idPrefix} />
          </div>
        );
      case "location":
        return <LocationFilter />;
      case "price":
        return range("minPriceEur", "maxPriceEur", t("price"), { decimal: true });
      case "rentalTerms":
        return (
          <div className="space-y-3">
            {petsFilterApplies(state) && select("petsAllowed", t("petsAllowed"), yesNo)}
            {select("utilitiesIncluded", t("utilitiesIncluded"), yesNo)}
            {labelled(
              "maxLeasePeriodMonths",
              t("maxLeasePeriod"),
              <DebouncedNumberInput value={one("maxLeasePeriodMonths")} onCommit={(v) => change({ maxLeasePeriodMonths: v })} placeholder={t("months")} ariaLabel={t("maxLeasePeriod")} />,
            )}
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
    // The generic "Suprafață" section's title already names its one field.
    const withLabels = section.id !== "area";
    return <div className="space-y-3">{section.items.map((entry) => item(entry, withLabels))}</div>;
  }

  return (
    <div className="space-y-2.5 text-ink-800">
      {scope === "details" && !propertyType && (
        <div className="rounded-xl border border-dashed border-ink-200 bg-white p-4">
          <PropertyTypePicker idPrefix={idPrefix} />
        </div>
      )}
      {sections.map((section) => {
        const titleKey = SEARCH_TITLES[section.id];
        return (
          <AccordionSection
            key={section.id}
            id={`${idPrefix}-${section.id}`}
            icon={ICONS[section.id]}
            title={titleKey ? t(titleKey) : tForm(`detailSections.${section.id}`)}
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
  );
}
