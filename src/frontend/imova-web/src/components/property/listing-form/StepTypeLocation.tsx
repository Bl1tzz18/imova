"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { getLocalitati, getRaioane, type Localitate, type Raion } from "@/lib/api/locations";
import { DealTypeTabs } from "./DealTypeTabs";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
const LISTING_TYPES = ["Sale", "Rent"] as const;

export function StepTypeLocation({
  propertyType,
  onPropertyTypeChange,
  listingType,
  onListingTypeChange,
  raionId,
  onRaionIdChange,
  localitateId,
  onLocalitateIdChange,
  defaultCountry,
  defaultStreetAddress,
}: {
  propertyType: string;
  onPropertyTypeChange: (value: string) => void;
  listingType: string;
  onListingTypeChange: (value: string) => void;
  raionId: string;
  onRaionIdChange: (value: string) => void;
  localitateId: string;
  onLocalitateIdChange: (value: string) => void;
  defaultCountry?: string;
  defaultStreetAddress?: string | null;
}) {
  const t = useTranslations("PropertyForm");
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");

  const [raioane, setRaioane] = useState<Raion[]>([]);
  const [localitati, setLocalitati] = useState<Localitate[]>([]);
  const [localitatiLoading, setLocalitatiLoading] = useState(false);

  useEffect(() => {
    getRaioane().then(setRaioane);
  }, []);

  useEffect(() => {
    if (!raionId) {
      setLocalitati([]);
      return;
    }
    setLocalitatiLoading(true);
    getLocalitati(raionId)
      .then(setLocalitati)
      .finally(() => setLocalitatiLoading(false));
  }, [raionId]);

  const selectedRaion = raioane.find((r) => r.id === raionId);
  const localitateLabel = selectedRaion?.localityLabel === "Sector" ? t("sectorLabel") : t("localitateLabel");

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
          <FieldLabel required>{t("raionLabel")}</FieldLabel>
          <SearchableSelect
            name="raionId"
            required
            value={raionId}
            onChange={onRaionIdChange}
            options={raioane.map((raion) => ({ id: raion.id, label: raion.nameRo }))}
            placeholder={t("raionPlaceholder")}
            searchPlaceholder={t("raionSearchPlaceholder")}
            noResultsText={t("searchNoResults")}
          />
        </label>
        <label className="block">
          <FieldLabel>{localitateLabel}</FieldLabel>
          <SearchableSelect
            name="localitateId"
            value={localitateId}
            disabled={!raionId || localitatiLoading}
            onChange={onLocalitateIdChange}
            options={localitati.map((localitate) => ({ id: localitate.id, label: localitate.nameRo }))}
            placeholder={raionId ? t("localitatePlaceholder") : t("localitatePlaceholderDisabled")}
            searchPlaceholder={t("localitateSearchPlaceholder")}
            noResultsText={t("searchNoResults")}
          />
        </label>
        <label className="block sm:col-span-2">
          <FieldLabel>{t("streetAddressLabel")}</FieldLabel>
          <TextInput
            name="streetAddress"
            maxLength={200}
            defaultValue={defaultStreetAddress ?? undefined}
            placeholder={t("streetAddressPlaceholder")}
          />
        </label>
      </div>

      <input type="hidden" name="country" value={defaultCountry ?? "Moldova"} />
    </div>
  );
}
