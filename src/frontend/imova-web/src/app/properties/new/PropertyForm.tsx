"use client";

import { useActionState, useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { FieldLabel, SelectInput, TextAreaInput, TextInput } from "@/components/ui/Field";
import { getFieldRequirement } from "@/lib/property/fieldRules";
import { createProperty, type CreatePropertyState } from "./actions";

const initialState: CreatePropertyState = {};

// No auth yet, so there's no logged-in user to attribute the listing to.
// Prefilled with the seeded demo user until real auth exists.
const DEMO_OWNER_ID = "33333333-3333-3333-3333-333333333333";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
const LISTING_TYPES = ["Rent", "Sale"] as const;
const CURRENT_YEAR = new Date().getFullYear();

export function PropertyForm() {
  const [state, formAction, pending] = useActionState(createProperty, initialState);
  const [propertyType, setPropertyType] = useState<string>("Apartment");
  const [listingType, setListingType] = useState<string>("Rent");

  const t = useTranslations("PropertyForm");
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");

  const req = (field: Parameters<typeof getFieldRequirement>[0]) =>
    getFieldRequirement(field, propertyType, listingType);

  return (
    <form action={formAction} className="space-y-8">
      <section>
        <h2 className="font-display text-lg font-medium text-ink-950">{t("sectionType")}</h2>
        <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
          <label className="block">
            <FieldLabel required>{t("listingTypeLabel")}</FieldLabel>
            <SelectInput
              name="listingType"
              required
              value={listingType}
              onChange={(e) => setListingType(e.target.value)}
            >
              {LISTING_TYPES.map((lt) => (
                <option key={lt} value={lt}>
                  {tListing(lt)}
                </option>
              ))}
            </SelectInput>
          </label>
          <label className="block">
            <FieldLabel required>{t("propertyTypeLabel")}</FieldLabel>
            <SelectInput
              name="propertyType"
              required
              value={propertyType}
              onChange={(e) => setPropertyType(e.target.value)}
            >
              {PROPERTY_TYPES.map((pt) => (
                <option key={pt} value={pt}>
                  {tType(pt)}
                </option>
              ))}
            </SelectInput>
          </label>
        </div>
      </section>

      <section className="border-t border-ink-100 pt-8">
        <h2 className="font-display text-lg font-medium text-ink-950">{t("sectionBasics")}</h2>
        <div className="mt-4 space-y-4">
          <label className="block">
            <FieldLabel required>{t("ownerIdLabel")}</FieldLabel>
            <TextInput name="ownerId" required defaultValue={DEMO_OWNER_ID} />
          </label>
          <label className="block">
            <FieldLabel required>{t("titleLabel")}</FieldLabel>
            <TextInput name="title" required maxLength={200} />
          </label>
          <label className="block">
            <FieldLabel required>{t("descriptionLabel")}</FieldLabel>
            <TextAreaInput name="description" required maxLength={4000} rows={4} />
          </label>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="block">
              <FieldLabel required>{t("priceLabel")}</FieldLabel>
              <TextInput name="price" type="number" min="0.01" step="0.01" required />
            </label>
            <label className="block">
              <FieldLabel required>{t("currencyLabel")}</FieldLabel>
              <TextInput
                name="currency"
                required
                minLength={3}
                maxLength={3}
                pattern="[A-Za-z]{3}"
                defaultValue="EUR"
                className="uppercase"
              />
            </label>
          </div>
        </div>
      </section>

      <section className="border-t border-ink-100 pt-8">
        <h2 className="font-display text-lg font-medium text-ink-950">{t("sectionDetails")}</h2>
        <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
          {req("area") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("area") === "required"}>{t("areaLabel")}</FieldLabel>
              <TextInput
                name="area"
                type="number"
                min="0.01"
                step="0.01"
                required={req("area") === "required"}
              />
            </label>
          )}
          {req("rooms") !== "hidden" && (
            <label className="block">
              <FieldLabel required={req("rooms") === "required"}>{t("roomsLabel")}</FieldLabel>
              <TextInput
                name="rooms"
                type="number"
                min="1"
                step="1"
                required={req("rooms") === "required"}
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
      </section>

      <section className="border-t border-ink-100 pt-8">
        <h2 className="font-display text-lg font-medium text-ink-950">{t("sectionLocation")}</h2>
        <div className="mt-4 space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="block">
              <FieldLabel required>{t("countryLabel")}</FieldLabel>
              <TextInput name="country" required maxLength={100} defaultValue="Moldova" />
            </label>
            <label className="block">
              <FieldLabel required>{t("cityLabel")}</FieldLabel>
              <TextInput name="city" required maxLength={100} />
            </label>
          </div>
          <label className="block">
            <FieldLabel>{t("districtLabel")}</FieldLabel>
            <TextInput name="district" maxLength={100} />
          </label>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="block">
              <FieldLabel required>{t("latitudeLabel")}</FieldLabel>
              <TextInput
                name="latitude"
                type="number"
                step="any"
                min="-90"
                max="90"
                required
                defaultValue="47.0105"
              />
            </label>
            <label className="block">
              <FieldLabel required>{t("longitudeLabel")}</FieldLabel>
              <TextInput
                name="longitude"
                type="number"
                step="any"
                min="-180"
                max="180"
                required
                defaultValue="28.8638"
              />
            </label>
          </div>
        </div>
      </section>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}

      <Button type="submit" disabled={pending} size="lg" className="w-full sm:w-auto">
        {pending ? t("saving") : t("publish")}
      </Button>
    </form>
  );
}
