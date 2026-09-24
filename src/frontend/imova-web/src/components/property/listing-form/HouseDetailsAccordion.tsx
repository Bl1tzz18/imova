"use client";

import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import { cn } from "@/lib/utils/cn";
import {
  ATTRIBUTE_SCHEMA,
  attributeInputName,
  isFieldVisible,
  type AttributeField,
  type ValueGetter,
} from "@/lib/property/attributeSchema";
import {
  HOUSE_SECTIONS,
  amenitiesInCategory,
  isSectionComplete,
  type HouseSection,
  type HouseSectionId,
} from "@/lib/property/houseSections";
import type { Amenity, PropertyDetails, TypeSpecificAttributes } from "@/types/listing";
import { AttributeCheckboxes, AttributeInput } from "./AttributeFields";

const CURRENT_YEAR = new Date().getFullYear();

const SECTION_ICONS: Record<HouseSectionId, ReactNode> = {
  structure: <path d="M3 11 12 4l9 7M5 10v10h14V10M10 20v-5h4v5" />,
  areas: <path d="M4 4h16v16H4zM4 9h5V4M15 20v-5h5" />,
  systems: <path d="M12 3c2.5 3 4 5.3 4 7.5a4 4 0 0 1-8 0C8 8.3 9.5 6 12 3ZM6 21h12M9 17h6" />,
  finishing: <path d="M4 20 14 10M14 4l6 6-3 3-6-6 3-3ZM4 20l2-6 4 4-6 2Z" />,
  comfort: <path d="M5 11V8a3 3 0 0 1 3-3h8a3 3 0 0 1 3 3v3M3 12a2 2 0 0 1 4 0v3h10v-3a2 2 0 0 1 4 0v6H3v-6ZM6 18v2M18 18v2" />,
  security: <path d="M12 3.5l7 2.6v5.2c0 5-3 8-7 9.2-4-1.2-7-4.2-7-9.2V6.1l7-2.6ZM9 12l2 2 4-4" />,
  leisure: <path d="M12 3a6 6 0 0 1 6 6H6a6 6 0 0 1 6-6ZM12 9v12M8 21h8M3 17c1.5 1 3 1 4.5 0s3-1 4.5 0 3 1 4.5 0 3-1 4.5 0" />,
};

function field(name: string): AttributeField {
  return ATTRIBUTE_SCHEMA.House.find((f) => f.name === name)!;
}

// The House "Details" step: the House attributes, the general Property fields that belong with
// them, and the House-relevant amenities, split into seven collapsible sections. Collapsed
// sections stay mounted (only visually hidden) so their inputs are still submitted and validated;
// an invalid field inside a collapsed section opens it (see onInvalidCapture), so the form's
// step validation can point at it.
export function HouseDetailsAccordion({
  property,
  initialAttributes,
  transactionType,
  amenities,
}: {
  property?: PropertyDetails;
  initialAttributes: TypeSpecificAttributes;
  transactionType: string;
  amenities: Amenity[];
}) {
  const t = useTranslations("PropertyForm");
  const tAmenity = useTranslations("Amenity");
  const containerRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState<Set<HouseSectionId>>(() => new Set(["structure"]));
  const [visited, setVisited] = useState<Set<HouseSectionId>>(() => new Set(["structure"]));
  const [heatingSystem, setHeatingSystem] = useState(
    typeof initialAttributes.heatingSystem === "string" ? initialAttributes.heatingSystem : "",
  );
  const [completed, setCompleted] = useState<Set<HouseSectionId>>(() => new Set());

  const selectedAmenityIds = new Set(property?.amenities.map((a) => a.id) ?? []);

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
    setCompleted(new Set(HOUSE_SECTIONS.filter((s) => isSectionComplete(s, get, visited.has(s.id))).map((s) => s.id)));
  }, [visited]);

  // Also re-run after heating details appear/disappear and when sections get visited.
  useEffect(recompute, [recompute, heatingSystem]);

  function setSectionOpen(id: HouseSectionId, isOpen: boolean) {
    setOpen((prev) => {
      const next = new Set(prev);
      if (isOpen) next.add(id);
      else next.delete(id);
      return next;
    });
    if (isOpen) setVisited((prev) => (prev.has(id) ? prev : new Set(prev).add(id)));
  }

  const heatingGet: ValueGetter = (name) => (name === attributeInputName("heatingSystem") ? heatingSystem : null);
  const completedCount = completed.size;

  function renderField(name: string) {
    const f = field(name);
    if (!isFieldVisible(f, heatingGet)) return null;
    if (f.kind === "bool" || f.kind === "flags") {
      return (
        <div key={name} className="sm:col-span-2">
          <AttributeCheckboxes field={f} initial={initialAttributes} />
        </div>
      );
    }
    return (
      <AttributeInput
        key={name}
        field={f}
        initial={initialAttributes}
        onChange={name === "heatingSystem" ? setHeatingSystem : undefined}
      />
    );
  }

  function renderSectionBody(section: HouseSection) {
    if (section.amenityCategory) {
      const items = amenitiesInCategory(amenities, section.amenityCategory, transactionType);
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
            <FieldLabel required>{t("houseTotalAreaLabel")}</FieldLabel>
            <TextInput
              name="totalAreaM2"
              type="number"
              min="0.01"
              step="0.01"
              required
              defaultValue={property?.totalAreaM2 ?? undefined}
            />
          </label>
        )}
        {section.attributeFields.map(renderField)}
      </div>
    );
  }

  return (
    <div ref={containerRef} onInput={recompute} onChange={recompute} className="space-y-3">
      <div>
        <div className="flex items-center justify-between text-xs text-ink-500">
          <span>{t("sectionsCompleted", { completed: completedCount, total: HOUSE_SECTIONS.length })}</span>
        </div>
        <div
          className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-ink-100"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={HOUSE_SECTIONS.length}
          aria-valuenow={completedCount}
          aria-label={t("sectionsCompleted", { completed: completedCount, total: HOUSE_SECTIONS.length })}
        >
          <div
            className="h-full rounded-full bg-brand-500 transition-[width] duration-300"
            style={{ width: `${(completedCount / HOUSE_SECTIONS.length) * 100}%` }}
          />
        </div>
      </div>

      {HOUSE_SECTIONS.map((section) => {
        const isOpen = open.has(section.id);
        const isDone = completed.has(section.id);
        const panelId = `house-section-${section.id}`;
        return (
          <div
            key={section.id}
            className={cn("rounded-xl border bg-white", isOpen ? "border-ink-200" : "border-ink-100")}
          >
            <button
              type="button"
              aria-expanded={isOpen}
              aria-controls={panelId}
              onClick={() => setSectionOpen(section.id, !isOpen)}
              className="flex w-full items-center gap-3 px-4 py-3 text-left"
            >
              <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-700">
                <svg
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  className="h-[18px] w-[18px]"
                >
                  {SECTION_ICONS[section.id]}
                </svg>
              </span>
              <span className="flex-1 font-hero text-sm font-bold text-ink-950">{t(`houseSections.${section.id}`)}</span>
              {isDone && (
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" className="h-4 w-4 text-brand-600">
                  <path d="m5 12.5 4.5 4.5L19 7.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              )}
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                className={cn("h-4 w-4 text-ink-400 transition-transform", isOpen && "rotate-180")}
              >
                <path d="m6 9 6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </button>
            <div
              id={panelId}
              // Stays mounted while collapsed so its inputs are still submitted/validated; a field
              // failing validation in here opens the section so the error can be shown.
              className={cn("border-t border-ink-100 px-4 py-4", !isOpen && "hidden")}
              onInvalidCapture={() => setSectionOpen(section.id, true)}
            >
              {renderSectionBody(section)}
            </div>
          </div>
        );
      })}
    </div>
  );
}
