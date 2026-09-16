"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { createProperty, type CreatePropertyState } from "./actions";

const initialState: CreatePropertyState = {};

// No auth yet, so there's no logged-in user to attribute the listing to.
// Prefilled with the seeded demo user until real auth exists.
const DEMO_OWNER_ID = "33333333-3333-3333-3333-333333333333";

export function PropertyForm() {
  const [state, formAction, pending] = useActionState(createProperty, initialState);
  const t = useTranslations("PropertyForm");
  const tType = useTranslations("PropertyType");
  const tListing = useTranslations("ListingType");

  return (
    <form
      action={formAction}
      style={{ display: "flex", flexDirection: "column", gap: "0.75rem", maxWidth: "28rem" }}
    >
      <label>
        {t("ownerIdLabel")}
        <input name="ownerId" required defaultValue={DEMO_OWNER_ID} />
      </label>
      <label>
        {t("titleLabel")}
        <input name="title" required maxLength={200} />
      </label>
      <label>
        {t("descriptionLabel")}
        <textarea name="description" required maxLength={4000} rows={4} />
      </label>
      <label>
        {t("propertyTypeLabel")}
        <select name="propertyType" required defaultValue="Apartment">
          <option value="Apartment">{tType("Apartment")}</option>
          <option value="House">{tType("House")}</option>
          <option value="Land">{tType("Land")}</option>
          <option value="Commercial">{tType("Commercial")}</option>
          <option value="Garage">{tType("Garage")}</option>
          <option value="Room">{tType("Room")}</option>
        </select>
      </label>
      <label>
        {t("listingTypeLabel")}
        <select name="listingType" required defaultValue="Rent">
          <option value="Rent">{tListing("Rent")}</option>
          <option value="Sale">{tListing("Sale")}</option>
        </select>
      </label>
      <label>
        {t("priceLabel")}
        <input name="price" type="number" min="0.01" step="0.01" required />
      </label>
      <label>
        {t("currencyLabel")}
        <input name="currency" required minLength={3} maxLength={3} pattern="[A-Za-z]{3}" defaultValue="EUR" />
      </label>
      <label>
        {t("countryLabel")}
        <input name="country" required maxLength={100} defaultValue="Moldova" />
      </label>
      <label>
        {t("cityLabel")}
        <input name="city" required maxLength={100} />
      </label>
      <label>
        {t("districtLabel")}
        <input name="district" maxLength={100} />
      </label>
      <label>
        {t("latitudeLabel")}
        <input name="latitude" type="number" step="any" min="-90" max="90" required defaultValue="47.0105" />
      </label>
      <label>
        {t("longitudeLabel")}
        <input name="longitude" type="number" step="any" min="-180" max="180" required defaultValue="28.8638" />
      </label>

      {state.error && <p style={{ color: "crimson" }}>{state.error}</p>}

      <button type="submit" disabled={pending}>
        {pending ? t("saving") : t("publish")}
      </button>
    </form>
  );
}
