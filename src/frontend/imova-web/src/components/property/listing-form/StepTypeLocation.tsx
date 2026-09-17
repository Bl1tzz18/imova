"use client";

import { useTranslations } from "next-intl";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { DealTypeTabs } from "./DealTypeTabs";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
const LISTING_TYPES = ["Sale", "Rent"] as const;

export function StepTypeLocation({
  propertyType,
  onPropertyTypeChange,
  listingType,
  onListingTypeChange,
}: {
  propertyType: string;
  onPropertyTypeChange: (value: string) => void;
  listingType: string;
  onListingTypeChange: (value: string) => void;
}) {
  const t = useTranslations("PropertyForm");
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step1Heading")}</h2>

      <div className="mt-5">
        <DealTypeTabs
          name="listingType"
          value={listingType}
          onChange={onListingTypeChange}
          options={LISTING_TYPES.map((lt) => ({ value: lt, label: tListing(lt) }))}
        />
      </div>

      <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label className="block">
          <FieldLabel required>{t("propertyTypeLabel")}</FieldLabel>
          <SelectInput
            name="propertyType"
            required
            value={propertyType}
            onChange={(e) => onPropertyTypeChange(e.target.value)}
          >
            {PROPERTY_TYPES.map((pt) => (
              <option key={pt} value={pt}>
                {tType(pt)}
              </option>
            ))}
          </SelectInput>
        </label>
        <label className="block">
          <FieldLabel required>{t("cityLabel")}</FieldLabel>
          <TextInput name="city" required maxLength={100} placeholder={t("cityPlaceholder")} />
        </label>
        <label className="block">
          <FieldLabel>{t("districtLabel")}</FieldLabel>
          <TextInput name="district" maxLength={100} placeholder={t("districtPlaceholder")} />
        </label>
      </div>

      <input type="hidden" name="country" value="Moldova" />
      {/* Stopgap until a map-based location picker / geocoding exists: every listing is
          pinned to Chișinău's center so the required lat/lng still reach the backend. */}
      <input type="hidden" name="latitude" value="47.0105" />
      <input type="hidden" name="longitude" value="28.8638" />
    </div>
  );
}
