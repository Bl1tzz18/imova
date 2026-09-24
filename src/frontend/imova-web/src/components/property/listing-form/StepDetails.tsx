"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, SelectInput, TextAreaInput, TextInput } from "@/components/ui/Field";
import { getAmenities } from "@/lib/api/amenities";
import { attributeSchemaFor, hasBuilding, usesGeneralCondition } from "@/lib/property/attributeSchema";
import { detailLayoutFor, selectableAmenities } from "@/lib/property/detailLayouts";
import { rentalFieldsForStep } from "@/lib/property/rentalFields";
import type { Amenity, Listing } from "@/types/listing";
import { AttributeInput } from "./AttributeFields";
import { DetailsAccordion } from "./DetailsAccordion";
import { PetsAllowedInput } from "./PetsAllowedInput";

const CURRENT_YEAR = new Date().getFullYear();
const CONDITIONS = ["New", "Renovated", "NeedsRepair", "GrayStructure", "RedStructure"] as const;

export function StepDetails({
  propertyType,
  transactionType,
  listing,
}: {
  propertyType: string;
  transactionType: string;
  listing?: Listing;
}) {
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
  const layout = detailLayoutFor(propertyType);
  // Only amenities that apply to this type ("furnished" included, for sale and rent alike).
  const offeredAmenities = selectableAmenities(amenities, propertyType);
  // Rental questions asked on this step (pets, for a rented home) — the accordion types ask them
  // in their own "Rental rules" section; the flat form (Room) asks them inline.
  const rentalDetailFields = rentalFieldsForStep("details", transactionType, propertyType);

  return (
    <div>
      <h2 className="font-hero text-xl font-bold text-ink-950">{t("step2Heading")}</h2>

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

        {layout ? (
          // Types with many details get collapsible sections (area, year built and the type's
          // amenities included); Garage and Room keep the flat form below.
          <DetailsAccordion
            key={propertyType}
            propertyType={propertyType}
            layout={layout}
            property={property}
            initialAttributes={initialAttributes}
            transactionType={transactionType}
            rental={listing?.rentalDetails}
            amenities={amenities}
          />
        ) : (
          <>
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
                </label>

                {hasBuilding(propertyType) && (
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
                {usesGeneralCondition(propertyType) && (
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
                )}

                {schema.map((field) => (
                  <AttributeInput key={field.name} field={field} initial={initialAttributes} />
                ))}
                {rentalDetailFields.includes("petsAllowed") && (
                  <PetsAllowedInput defaultValue={listing?.rentalDetails?.petsAllowed} />
                )}
              </div>
            </div>

            {offeredAmenities.length > 0 && (
              <fieldset>
                <legend className="mb-2 text-sm font-medium text-ink-700">{t("amenitiesLabel")}</legend>
                <div className="grid grid-cols-1 gap-x-5 gap-y-2 sm:grid-cols-2 lg:grid-cols-3">
                  {offeredAmenities.map((amenity) => (
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
          </>
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
