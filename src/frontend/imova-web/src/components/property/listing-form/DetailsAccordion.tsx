"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { squareMetersToAri } from "@/lib/listing/view";
import {
  GENERAL_CONDITIONS,
  attributeSchemaFor,
  controllingFields,
  isFieldVisible,
  type ValueGetter,
} from "@/lib/property/attributeSchema";
import {
  amenitiesForSection,
  isAmenitySection,
  isProximitySection,
  isSectionComplete,
  sectionsFor,
  selectableProximities,
  type DetailLayout,
  type DetailSection,
  type DetailSectionId,
} from "@/lib/property/detailLayouts";
import type { Amenity, PropertyDetails, Proximity, RentalDetails, TypeSpecificAttributes } from "@/types/listing";
import { AccordionSection } from "@/components/ui/AccordionSection";
import { SECTION_ICONS } from "@/components/property/sectionIcons";
import { AttributeInput } from "./AttributeFields";
import { PetsAllowedInput } from "./PetsAllowedInput";

const CURRENT_YEAR = new Date().getFullYear();


// The "Details" step for every property type (see DETAIL_LAYOUTS): the type's attributes, the
// general Property fields that belong with them, its amenities and what it's near (proximities),
// split into collapsible sections. Collapsed sections stay mounted (only visually hidden) so their
// inputs are still submitted and validated; an invalid field inside a collapsed section opens it
// (see onInvalidCapture), so the form's step validation can point at it.
export function DetailsAccordion({
  propertyType,
  layout,
  property,
  initialAttributes,
  transactionType,
  rental,
  amenities,
  proximities,
}: {
  propertyType: string;
  layout: DetailLayout;
  property?: PropertyDetails;
  initialAttributes: TypeSpecificAttributes;
  transactionType: string;
  rental?: RentalDetails | null;
  amenities: Amenity[];
  proximities: Proximity[];
}) {
  const t = useTranslations("PropertyForm");
  const tAmenity = useTranslations("Amenity");
  const tProximity = useTranslations("Proximity");
  const tCondition = useTranslations("Condition");
  const containerRef = useRef<HTMLDivElement>(null);
  const firstSection = layout.sections[0].id;
  const [open, setOpen] = useState<Set<DetailSectionId>>(() => new Set([firstSection]));
  const [visited, setVisited] = useState<Set<DetailSectionId>>(() => new Set([firstSection]));
  const [completed, setCompleted] = useState<Set<DetailSectionId>>(() => new Set());
  const [area, setArea] = useState(property ? String(property.totalAreaM2) : "");

  // Live values of the fields that decide whether others are shown (heatingSystem, plotType,
  // spaceType), seeded from the listing being edited.
  const [controlling, setControlling] = useState<Record<string, string>>(() =>
    Object.fromEntries(
      controllingFields(propertyType).map((name) => {
        const value = initialAttributes[name];
        return [name, typeof value === "string" ? value : ""];
      }),
    ),
  );

  const schema = attributeSchemaFor(propertyType);
  // Rental-only sections (pets) only exist for a rental.
  const sections = sectionsFor(layout, transactionType);
  const selectedAmenityIds = new Set(property?.amenities.map((a) => a.id) ?? []);
  const selectedProximityIds = new Set(property?.proximities.map((p) => p.id) ?? []);
  const visibilityGet: ValueGetter = (name) => controlling[name.replace(/^attr\./, "")] ?? null;

  // Completion is read straight from the form's current values, so it covers every input
  // (collapsed sections included) without mirroring each one in React state.
  const recompute = useCallback(() => {
    const form = containerRef.current?.closest("form");
    if (!form) return;
    const data = new FormData(form);
    const get: ValueGetter = (name) => {
      const value = data.get(name);
      return typeof value === "string" ? value : null;
    };
    setCompleted(
      new Set(
        sectionsFor(layout, transactionType)
          .filter((s) => isSectionComplete(s, propertyType, get, visited.has(s.id)))
          .map((s) => s.id),
      ),
    );
  }, [layout, propertyType, transactionType, visited]);

  // Also re-run after conditional fields appear/disappear and when sections get visited.
  useEffect(recompute, [recompute, controlling]);

  function setSectionOpen(id: DetailSectionId, isOpen: boolean) {
    setOpen((prev) => {
      const next = new Set(prev);
      if (isOpen) next.add(id);
      else next.delete(id);
      return next;
    });
    if (isOpen) setVisited((prev) => (prev.has(id) ? prev : new Set(prev).add(id)));
  }

  function renderField(name: string) {
    const field = schema.find((f) => f.name === name);
    if (!field || !isFieldVisible(field, visibilityGet)) return null;
    const isControlling = name in controlling;
    return (
      <AttributeInput
        key={name}
        field={field}
        initial={initialAttributes}
        onChange={isControlling ? (value) => setControlling((prev) => ({ ...prev, [name]: value })) : undefined}
      />
    );
  }

  function renderSectionBody(section: DetailSection) {
    if (section.rentalFields) {
      return (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {section.rentalFields.includes("petsAllowed") && <PetsAllowedInput defaultValue={rental?.petsAllowed} />}
        </div>
      );
    }

    if (isProximitySection(section)) {
      return (
        <div className="grid grid-cols-1 gap-x-5 gap-y-2 sm:grid-cols-2">
          {selectableProximities(proximities, propertyType).map((proximity) => (
            <Checkbox
              key={proximity.id}
              name="proximityIds"
              value={proximity.id}
              defaultChecked={selectedProximityIds.has(proximity.id)}
              className="text-ink-700"
            >
              {tProximity.has(proximity.key) ? tProximity(proximity.key) : proximity.labelRo}
            </Checkbox>
          ))}
        </div>
      );
    }

    if (isAmenitySection(section)) {
      const items = amenitiesForSection(layout, section, amenities, propertyType);
      return (
        <div className="grid grid-cols-1 gap-x-5 gap-y-2 sm:grid-cols-2">
          {items.map((amenity) => (
            <Checkbox
              key={amenity.id}
              name="amenityIds"
              value={amenity.id}
              defaultChecked={selectedAmenityIds.has(amenity.id)}
              className="text-ink-700"
            >
              {tAmenity.has(amenity.key) ? tAmenity(amenity.key) : amenity.labelRo}
            </Checkbox>
          ))}
        </div>
      );
    }

    const areaNumber = Number(area);
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {section.coreFields.includes("yearBuilt") && (
          <label className="block">
            <FieldLabel>{t("yearBuiltLabel")}</FieldLabel>
            <TextInput
              name="yearBuilt"
              type="number"
              min="1800"
              max={CURRENT_YEAR + 1}
              step="1"
              defaultValue={property?.yearBuilt ?? undefined}
            />
          </label>
        )}
        {section.coreFields.includes("totalAreaM2") && (
          <label className="block">
            <FieldLabel required>{t(layout.totalAreaLabel)}</FieldLabel>
            <TextInput
              name="totalAreaM2"
              type="number"
              min="0.01"
              step="0.01"
              required
              value={area}
              onChange={(e) => setArea(e.target.value)}
            />
            {propertyType === "Land" && areaNumber > 0 && (
              <span className="mt-1 block text-xs text-ink-500">
                {t("areaInAri", { ari: squareMetersToAri(areaNumber) })}
              </span>
            )}
          </label>
        )}
        {section.coreFields.includes("condition") && (
          <label className="block">
            <FieldLabel>{t("conditionLabel")}</FieldLabel>
            <SelectInput name="condition" defaultValue={property?.condition ?? ""}>
              <option value="">{t("notSpecified")}</option>
              {GENERAL_CONDITIONS.map((condition) => (
                <option key={condition} value={condition}>
                  {tCondition(condition)}
                </option>
              ))}
            </SelectInput>
          </label>
        )}
        {section.attributeFields.map(renderField)}
      </div>
    );
  }

  const total = sections.length;
  const completedCount = completed.size;
  const progressLabel = t("sectionsCompleted", { completed: completedCount, total });

  return (
    <div ref={containerRef} onInput={recompute} onChange={recompute} className="space-y-3">
      <div>
        <div className="flex items-center justify-between text-xs text-ink-500">
          <span>{progressLabel}</span>
        </div>
        <div
          className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-ink-100"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={total}
          aria-valuenow={completedCount}
          aria-label={progressLabel}
        >
          <div
            className="h-full rounded-full bg-brand-500 transition-[width] duration-300"
            style={{ width: `${(completedCount / total) * 100}%` }}
          />
        </div>
      </div>

      {sections.map((section) => {
        const isOpen = open.has(section.id);
        const isDone = completed.has(section.id);
        return (
          <AccordionSection
            key={section.id}
            id={`detail-${section.id}`}
            icon={SECTION_ICONS[section.id]}
            title={t(`detailSections.${section.id}`)}
            status={
              isDone && (
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" className="h-4 w-4 text-brand-600">
                  <path d="m5 12.5 4.5 4.5L19 7.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              )
            }
            open={isOpen}
            onToggle={() => setSectionOpen(section.id, !isOpen)}
            // A field failing validation in a collapsed section opens it so the error can be shown.
            onInvalidCapture={() => setSectionOpen(section.id, true)}
          >
            {renderSectionBody(section)}
          </AccordionSection>
        );
      })}
    </div>
  );
}
