"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { FieldLabel, TextAreaInput, TextInput } from "@/components/ui/Field";
import { getAmenities } from "@/lib/api/amenities";
import { getProximities } from "@/lib/api/proximities";
import { detailLayoutFor } from "@/lib/property/detailLayouts";
import type { Amenity, Listing, Proximity } from "@/types/listing";
import { DetailsAccordion } from "./DetailsAccordion";

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

  const [amenities, setAmenities] = useState<Amenity[]>([]);
  useEffect(() => {
    getAmenities()
      .then(setAmenities)
      .catch(() => setAmenities([]));
  }, []);

  const [proximities, setProximities] = useState<Proximity[]>([]);
  useEffect(() => {
    getProximities()
      .then(setProximities)
      .catch(() => setProximities([]));
  }, []);

  const property = listing?.property;

  // Type-specific values only pre-fill while the form still shows the listing's own type —
  // switching to another type starts that type's fields blank (their keys don't carry over).
  const initialAttributes = property?.propertyType === propertyType ? property.typeSpecificAttributes : {};
  const layout = detailLayoutFor(propertyType);

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

        {layout && (
          // Collapsible sections: area, year built/condition, the type's attributes, amenities,
          // proximities and (for a rented home) pets.
          <DetailsAccordion
            key={propertyType}
            propertyType={propertyType}
            layout={layout}
            property={property}
            initialAttributes={initialAttributes}
            transactionType={transactionType}
            rental={listing?.rentalDetails}
            amenities={amenities}
            proximities={proximities}
          />
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
