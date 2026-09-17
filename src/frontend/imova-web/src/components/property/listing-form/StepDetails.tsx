"use client";

import { useTranslations } from "next-intl";
import { FieldLabel, SelectInput, TextAreaInput, TextInput } from "@/components/ui/Field";
import { getFieldRequirement, type DetailFieldName } from "@/lib/property/fieldRules";

const CURRENT_YEAR = new Date().getFullYear();

export function StepDetails({
  propertyType,
  listingType,
}: {
  propertyType: string;
  listingType: string;
}) {
  const t = useTranslations("PropertyForm");
  const req = (field: DetailFieldName) => getFieldRequirement(field, propertyType, listingType);

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step2Heading")}</h2>

      <div className="mt-5 space-y-4">
        <label className="block">
          <FieldLabel required>{t("titleLabel")}</FieldLabel>
          <TextInput name="title" required maxLength={200} placeholder={t("titlePlaceholder")} />
        </label>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {req("area") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("area") === "required"}>{t("areaLabel")}</FieldLabel>
              <TextInput name="area" type="number" min="0.01" step="0.01" required={req("area") === "required"} />
            </label>
          )}
          {req("rooms") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("rooms") === "required"}>{t("roomsLabel")}</FieldLabel>
              <TextInput name="rooms" type="number" min="1" step="1" required={req("rooms") === "required"} />
            </label>
          )}
          {req("floor") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("floor") === "required"}>{t("floorLabel")}</FieldLabel>
              <TextInput
                name="floor"
                type="number"
                min="-5"
                max="200"
                step="1"
                required={req("floor") === "required"}
              />
            </label>
          )}
          {req("totalFloors") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("totalFloors") === "required"}>{t("totalFloorsLabel")}</FieldLabel>
              <TextInput
                name="totalFloors"
                type="number"
                min="1"
                step="1"
                required={req("totalFloors") === "required"}
              />
            </label>
          )}
          {req("yearBuilt") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("yearBuilt") === "required"}>{t("yearBuiltLabel")}</FieldLabel>
              <TextInput
                name="yearBuilt"
                type="number"
                min="1800"
                max={CURRENT_YEAR + 1}
                step="1"
                required={req("yearBuilt") === "required"}
              />
            </label>
          )}
          {req("bathrooms") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("bathrooms") === "required"}>{t("bathroomsLabel")}</FieldLabel>
              <TextInput
                name="bathrooms"
                type="number"
                min="0"
                step="1"
                required={req("bathrooms") === "required"}
              />
            </label>
          )}
          {req("furnished") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("furnished") === "required"}>{t("furnishedLabel")}</FieldLabel>
              <SelectInput name="furnished" defaultValue="" required={req("furnished") === "required"}>
                <option value="">{t("notSpecified")}</option>
                <option value="true">{t("yes")}</option>
                <option value="false">{t("no")}</option>
              </SelectInput>
            </label>
          )}
          {req("parkingAvailable") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("parkingAvailable") === "required"}>
                {t("parkingAvailableLabel")}
              </FieldLabel>
              <SelectInput
                name="parkingAvailable"
                defaultValue=""
                required={req("parkingAvailable") === "required"}
              >
                <option value="">{t("notSpecified")}</option>
                <option value="true">{t("yes")}</option>
                <option value="false">{t("no")}</option>
              </SelectInput>
            </label>
          )}
          {req("petsAllowed") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("petsAllowed") === "required"}>{t("petsAllowedLabel")}</FieldLabel>
              <SelectInput name="petsAllowed" defaultValue="" required={req("petsAllowed") === "required"}>
                <option value="">{t("notSpecified")}</option>
                <option value="true">{t("yes")}</option>
                <option value="false">{t("no")}</option>
              </SelectInput>
            </label>
          )}
        </div>

        <label className="block">
          <FieldLabel required>{t("descriptionLabel")}</FieldLabel>
          <TextAreaInput
            name="description"
            required
            maxLength={4000}
            rows={5}
            placeholder={t("descriptionPlaceholder")}
          />
        </label>
      </div>
    </div>
  );
}
