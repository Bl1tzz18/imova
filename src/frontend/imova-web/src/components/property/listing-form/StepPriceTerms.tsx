"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { DateInput } from "@/components/ui/DateInput";
import { FieldLabel, PriceInput, SelectInput, TextInput } from "@/components/ui/Field";
import { rentalFieldsForStep, rentalInputName } from "@/lib/property/rentalFields";
import type { Listing } from "@/types/listing";

// Mirrors the backend's Currency enum (Imova.Domain.Listings.Currency) — the backend still
// validates independently. Keep the two in sync.
const SUPPORTED_CURRENCIES = ["EUR", "MDL", "USD"] as const;
const DEFAULT_CURRENCY = "EUR";

// Step 4: price and, for a rental, the lease terms. Who to contact is the next step (StepContact).
export function StepPriceTerms({ transactionType, listing }: { transactionType: string; listing?: Listing }) {
  const t = useTranslations("PropertyForm");
  const tAttr = useTranslations("Attributes");
  const rental = listing?.rentalDetails;
  // Tracked so the deposit field can show the same currency as the price, live.
  const [currency, setCurrency] = useState(listing?.price.currency ?? DEFAULT_CURRENCY);
  // Pets are asked on the Details step; furnishing is the "furnished" amenity there.
  const rentalTerms = rentalFieldsForStep("priceTerms", transactionType, listing?.property.propertyType ?? "");

  return (
    <div>
      <h2 className="font-hero text-xl font-bold text-ink-950">{t("step4Heading")}</h2>

      <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label className="block">
          <FieldLabel required>{t("priceLabel")}</FieldLabel>
          <PriceInput name="price" required defaultValue={listing?.price.amount} />
        </label>
        <label className="block">
          <FieldLabel required>{t("currencyLabel")}</FieldLabel>
          <SelectInput name="currency" required value={currency} onChange={(e) => setCurrency(e.target.value)}>
            {SUPPORTED_CURRENCIES.map((currency) => (
              <option key={currency} value={currency}>
                {currency}
              </option>
            ))}
          </SelectInput>
        </label>
      </div>
      <Checkbox
        name="isNegotiable"
        value="true"
        defaultChecked={listing?.price.isNegotiable ?? false}
        className="mt-3 text-ink-700"
      >
        {t("isNegotiableLabel")}
      </Checkbox>

      {rentalTerms.length > 0 && (
        <fieldset className="mt-7">
          <legend className="font-hero text-base font-bold text-ink-950">{t("rentalTermsHeading")}</legend>
          <div className="mt-3 grid grid-cols-1 gap-4 sm:grid-cols-2">
            {rentalTerms.includes("minLeasePeriodMonths") && (
              <label className="block">
                <FieldLabel>{t("minLeasePeriodLabel")}</FieldLabel>
                <TextInput
                  name={rentalInputName("minLeasePeriodMonths")}
                  type="number"
                  min="1"
                  max="120"
                  step="1"
                  defaultValue={rental?.minLeasePeriodMonths ?? undefined}
                />
              </label>
            )}
            {rentalTerms.includes("securityDepositAmount") && (
              <label className="block">
                <FieldLabel>{t("securityDepositLabel")}</FieldLabel>
                {/* The deposit is in the listing's currency — shown right on the field. */}
                <div className="relative">
                  <TextInput
                    name={rentalInputName("securityDepositAmount")}
                    type="number"
                    min="0"
                    step="0.01"
                    defaultValue={rental?.securityDepositAmount ?? undefined}
                    className="pr-14"
                  />
                  <span className="pointer-events-none absolute inset-y-0 right-3.5 flex items-center text-sm font-medium text-ink-500">
                    {currency}
                  </span>
                </div>
              </label>
            )}
            {rentalTerms.includes("availableFrom") && (
              <label className="block">
                <FieldLabel>{t("availableFromLabel")}</FieldLabel>
                <DateInput
                  name={rentalInputName("availableFrom")}
                  defaultValue={rental?.availableFrom}
                  placeholder={t("datePlaceholder")}
                  invalidMessage={t("invalidDate")}
                  pickerLabel={t("openCalendar")}
                />
              </label>
            )}
          </div>
          {rentalTerms.includes("utilitiesIncluded") && (
            // A Yes/No dropdown like the app's other yes/no questions; still the same boolean, and
            // still optional ("No" unless the owner says otherwise).
            <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <label className="block">
                <FieldLabel>{t("utilitiesIncludedLabel")}</FieldLabel>
                <SelectInput
                  name={rentalInputName("utilitiesIncluded")}
                  defaultValue={String(rental?.utilitiesIncluded ?? false)}
                >
                  <option value="true">{tAttr("yes")}</option>
                  <option value="false">{tAttr("no")}</option>
                </SelectInput>
              </label>
            </div>
          )}
        </fieldset>
      )}
    </div>
  );
}
