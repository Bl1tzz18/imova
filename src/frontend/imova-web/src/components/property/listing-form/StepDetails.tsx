"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, SelectInput, TextAreaInput, TextInput } from "@/components/ui/Field";
import { getAmenities } from "@/lib/api/amenities";
import { squareMetersToAri } from "@/lib/listing/view";
import { attributeSchemaFor, hasBuilding } from "@/lib/property/attributeSchema";
import type { Amenity, Listing } from "@/types/listing";
import { AttributeCheckboxes, AttributeInput } from "./AttributeFields";

const CURRENT_YEAR = new Date().getFullYear();
const CONDITIONS = ["New", "Renovated", "NeedsRepair", "GrayStructure", "RedStructure"] as const;

export function StepDetails({ propertyType, listing }: { propertyType: string; listing?: Listing }) {
  const t = useTranslations("PropertyForm");
  const tCondition = useTranslations("Condition");
  const tAmenity = useTranslations("Amenity");

  const [amenities, setAmenities] = useState<Amenity[]>([]);
  useEffect(() => {
    getAmenities()
      .then(setAmenities)
      .catch(() => setAmenities([]));
  }, []);

  const property = listing?.property;
  const [area, setArea] = useState(property ? String(property.totalAreaM2) : "");

  // Type-specific values only pre-fill while the form still shows the listing's own type —
  // switching to another type starts that type's fields blank (their keys don't carry over).
  const initialAttributes = property?.propertyType === propertyType ? property.typeSpecificAttributes : {};
  const selectedAmenityIds = new Set(property?.amenities.map((a) => a.id) ?? []);
  const schema = attributeSchemaFor(propertyType);
  const scalarFields = schema.filter((f) => f.kind !== "bool" && f.kind !== "flags");
  const checkboxFields = schema.filter((f) => f.kind === "bool" || f.kind === "flags");
  const isLand = propertyType === "Land";
  const areaNumber = Number(area);

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step2Heading")}</h2>

      <div className="mt-5 space-y-5">
        <label className="block">
          <FieldLabel required>{t("titleLabel")}</FieldLabel>
          <TextInput
            name="title"
            required
            maxLength={200}
            defaultValue={listing?.title}
            placeholder={t("titlePlaceholder")}
          />
        </label>

        {/* Keyed by type so switching type remounts every type-dependent input with fresh defaults. */}
        <div key={propertyType} className="space-y-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="block">
              <FieldLabel required>{t("areaLabel")}</FieldLabel>
              <TextInput
                name="totalAreaM2"
                type="number"
                min="0.01"
                step="0.01"
                value={area}
                onChange={(e) => setArea(e.target.value)}
                required
              />
              {isLand && areaNumber > 0 && (
                <span className="mt-1 block text-xs text-ink-500">
                  {t("areaInAri", { ari: squareMetersToAri(areaNumber) })}
                </span>
              )}
            </label>

            {hasBuilding(propertyType) && (
              <>
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
                <label className="block">
                  <FieldLabel>{t("conditionLabel")}</FieldLabel>
                  <SelectInput name="condition" defaultValue={property?.condition ?? ""}>
                    <option value="">{t("notSpecified")}</option>
                    {CONDITIONS.map((condition) => (
                      <option key={condition} value={condition}>
                        {tCondition(condition)}
                      </option>
                    ))}
                  </SelectInput>
                </label>
              </>
            )}

            {scalarFields.map((field) => (
              <AttributeInput key={field.name} field={field} initial={initialAttributes} />
            ))}
          </div>

          {checkboxFields.map((field) => (
            <AttributeCheckboxes key={field.name} field={field} initial={initialAttributes} />
          ))}
        </div>

        {amenities.length > 0 && (
          <fieldset>
            <legend className="mb-2 text-sm font-medium text-ink-700">{t("amenitiesLabel")}</legend>
            <div className="grid grid-cols-1 gap-x-5 gap-y-2 sm:grid-cols-2 lg:grid-cols-3">
              {amenities.map((amenity) => (
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
          </fieldset>
        )}

        <label className="block">
          <FieldLabel required>{t("descriptionLabel")}</FieldLabel>
          <TextAreaInput
            name="description"
            required
            maxLength={4000}
            rows={5}
            defaultValue={listing?.description}
            placeholder={t("descriptionPlaceholder")}
          />
        </label>
      </div>
    </div>
  );
}
