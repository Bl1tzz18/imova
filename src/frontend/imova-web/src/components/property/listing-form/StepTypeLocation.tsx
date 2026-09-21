"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { cn } from "@/lib/utils/cn";
import {
  getChisinauSectors,
  getLocalitati,
  getRaioane,
  type ChisinauSector,
  type Localitate,
  type Raion,
} from "@/lib/api/locations";
import { DealTypeTabs } from "./DealTypeTabs";
import { StreetAddressAutocomplete } from "./StreetAddressAutocomplete";

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
  chisinauSectorId,
  onChisinauSectorIdChange,
  defaultCountry,
  defaultStreetAddress,
  defaultBuildingNumber,
}: {
  propertyType: string;
  onPropertyTypeChange: (value: string) => void;
  listingType: string;
  onListingTypeChange: (value: string) => void;
  raionId: string;
  onRaionIdChange: (value: string) => void;
  localitateId: string;
  onLocalitateIdChange: (value: string) => void;
  chisinauSectorId: string;
  onChisinauSectorIdChange: (value: string) => void;
  defaultCountry?: string;
  defaultStreetAddress?: string | null;
  defaultBuildingNumber?: string | null;
}) {
  const t = useTranslations("PropertyForm");
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");

  const [raioane, setRaioane] = useState<Raion[]>([]);
  const [localitati, setLocalitati] = useState<Localitate[]>([]);
  const [localitatiLoading, setLocalitatiLoading] = useState(false);
  const [chisinauSectors, setChisinauSectors] = useState<ChisinauSector[]>([]);

  useEffect(() => {
    getRaioane().then(setRaioane);
    getChisinauSectors().then(setChisinauSectors);
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
  // Chișinău gets a single-choice Sector/Suburbie control instead of one relabeled dropdown —
  // every other raion keeps the single Localitate dropdown exactly as before. Sector (the fixed
  // informal-neighborhood list) and Suburbie (the real CUATM localitate list) are mutually
  // exclusive — a listing can't physically be in both at once — so only one dropdown is ever
  // shown/submitted, never both.
  const isChisinau = selectedRaion?.localityLabel === "Sector";
  const [chisinauKind, setChisinauKind] = useState<"sector" | "suburbie">(() =>
    localitateId ? "suburbie" : "sector",
  );
  // Chișinău's flattened Localitate list also contains its 5 official CUATM sectors ("Sectorul
  // Botanica", etc.) alongside the real suburb towns/communes — those are already covered by the
  // separate Sector tab/dropdown above, so exclude them here to avoid showing the same places
  // twice under two different names.
  const suburbieOptions = localitati.filter((localitate) => !localitate.nameRo.startsWith("Sectorul "));

  function handleChisinauKindChange(kind: "sector" | "suburbie") {
    setChisinauKind(kind);
    // Clear whichever field is about to be hidden, so switching tabs can never leave a stale
    // selection behind that later gets submitted alongside the new one.
    if (kind === "sector") {
      onLocalitateIdChange("");
    } else {
      onChisinauSectorIdChange("");
    }
  }

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
        {isChisinau ? (
          <div className="block">
            <FieldLabel>{t("chisinauLocationLabel")}</FieldLabel>
            <div className="mb-2 inline-flex gap-1 rounded-xl border border-ink-100 bg-ink-50 p-1">
              {(["sector", "suburbie"] as const).map((kind) => (
                <button
                  key={kind}
                  type="button"
                  onClick={() => handleChisinauKindChange(kind)}
                  className={cn(
                    "rounded-lg px-3 py-1.5 text-sm font-medium transition-colors",
                    chisinauKind === kind ? "bg-white text-ink-900 shadow-sm" : "text-ink-500 hover:text-ink-900",
                  )}
                >
                  {kind === "sector" ? t("sectorLabel") : t("suburbieLabel")}
                </button>
              ))}
            </div>
            {chisinauKind === "sector" ? (
              <SearchableSelect
                name="chisinauSectorId"
                value={chisinauSectorId}
                onChange={onChisinauSectorIdChange}
                options={chisinauSectors.map((sector) => ({ id: sector.id, label: sector.name }))}
                placeholder={t("chisinauSectorPlaceholder")}
                searchPlaceholder={t("chisinauSectorSearchPlaceholder")}
                noResultsText={t("searchNoResults")}
              />
            ) : (
              <SearchableSelect
                name="localitateId"
                value={localitateId}
                disabled={localitatiLoading}
                onChange={onLocalitateIdChange}
                options={suburbieOptions.map((localitate) => ({ id: localitate.id, label: localitate.nameRo }))}
                placeholder={t("suburbiePlaceholder")}
                searchPlaceholder={t("suburbieSearchPlaceholder")}
                noResultsText={t("searchNoResults")}
              />
            )}
          </div>
        ) : (
          <label className="block">
            <FieldLabel>{t("localitateLabel")}</FieldLabel>
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
        )}
        <div className="grid grid-cols-1 gap-4 sm:col-span-2 sm:grid-cols-3">
          <label className="block sm:col-span-2">
            <FieldLabel required>{t("streetAddressLabel")}</FieldLabel>
            <StreetAddressAutocomplete
              raionId={raionId || undefined}
              localitateId={localitateId || undefined}
              defaultValue={defaultStreetAddress}
              placeholder={t("streetAddressPlaceholder")}
              required
            />
          </label>
          <label className="block">
            <FieldLabel>{t("buildingNumberLabel")}</FieldLabel>
            <TextInput
              name="buildingNumber"
              maxLength={20}
              defaultValue={defaultBuildingNumber ?? undefined}
              placeholder={t("buildingNumberPlaceholder")}
            />
          </label>
        </div>
      </div>

      <input type="hidden" name="country" value={defaultCountry ?? "Moldova"} />
    </div>
  );
}
