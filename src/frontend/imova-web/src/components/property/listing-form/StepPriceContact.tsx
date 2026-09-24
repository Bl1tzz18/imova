"use client";

import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, PriceInput, SelectInput, TextInput } from "@/components/ui/Field";
import { cn } from "@/lib/utils/cn";
import type { Listing, Publisher } from "@/types/listing";

// Mirrors the backend's Currency enum (Imova.Domain.Listings.Currency) — the backend still
// validates independently. Keep the two in sync.
const SUPPORTED_CURRENCIES = ["EUR", "MDL", "USD"] as const;
const DEFAULT_CURRENCY = "EUR";
const FURNISHED_STATUSES = ["Unfurnished", "PartiallyFurnished", "Furnished"] as const;

export function StepPriceContact({
  transactionType,
  listing,
  publishers,
}: {
  transactionType: string;
  listing?: Listing;
  // Only passed in create mode — a listing's publisher can't be changed afterwards.
  publishers: Publisher[];
}) {
  const t = useTranslations("PropertyForm");
  const tFurnished = useTranslations("FurnishedStatus");
  const rental = listing?.rentalDetails;

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step4Heading")}</h2>

      <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label className="block">
          <FieldLabel required>{t("priceLabel")}</FieldLabel>
          <PriceInput name="price" required defaultValue={listing?.price.amount} />
        </label>
        <label className="block">
          <FieldLabel required>{t("currencyLabel")}</FieldLabel>
          <SelectInput name="currency" required defaultValue={listing?.price.currency ?? DEFAULT_CURRENCY}>
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

      {transactionType === "Rent" && (
        <fieldset className="mt-7">
          <legend className="font-display text-base font-medium text-ink-950">{t("rentalTermsHeading")}</legend>
          <div className="mt-3 grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="block">
              <FieldLabel>{t("furnishedStatusLabel")}</FieldLabel>
              <SelectInput name="rental.furnishedStatus" defaultValue={rental?.furnishedStatus ?? "Unfurnished"}>
                {FURNISHED_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {tFurnished(status)}
                  </option>
                ))}
              </SelectInput>
            </label>
            <label className="block">
              <FieldLabel>{t("minLeasePeriodLabel")}</FieldLabel>
              <TextInput
                name="rental.minLeasePeriodMonths"
                type="number"
                min="1"
                max="120"
                step="1"
                defaultValue={rental?.minLeasePeriodMonths ?? undefined}
              />
            </label>
            <label className="block">
              <FieldLabel>{t("securityDepositLabel")}</FieldLabel>
              <TextInput
                name="rental.securityDepositAmount"
                type="number"
                min="0"
                step="0.01"
                defaultValue={rental?.securityDepositAmount ?? undefined}
              />
            </label>
            <label className="block">
              <FieldLabel>{t("availableFromLabel")}</FieldLabel>
              <TextInput
                name="rental.availableFrom"
                type="date"
                defaultValue={rental?.availableFrom ? rental.availableFrom.slice(0, 10) : undefined}
              />
            </label>
          </div>
          <div className="mt-3 flex flex-wrap gap-x-6 gap-y-2">
            <Checkbox
              name="rental.utilitiesIncluded"
              value="true"
              defaultChecked={rental?.utilitiesIncluded ?? false}
              className="text-ink-700"
            >
              {t("utilitiesIncludedLabel")}
            </Checkbox>
            <Checkbox
              name="rental.petsAllowed"
              value="true"
              defaultChecked={rental?.petsAllowed ?? false}
              className="text-ink-700"
            >
              {t("petsAllowedLabel")}
            </Checkbox>
          </div>
        </fieldset>
      )}

      {/* Only offered when there's an actual choice: an Individual publisher always exists, and
          omitting publisherId publishes under it, so a user without an agency sees nothing here. */}
      {publishers.length > 1 && (
        <fieldset className="mt-7">
          <legend className="font-display text-base font-medium text-ink-950">{t("publishAsLabel")}</legend>
          <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {publishers.map((publisher) => (
              <label
                key={publisher.id}
                className={cn(
                  "flex cursor-pointer items-start gap-3 rounded-xl border border-ink-200 px-4 py-3 text-sm",
                  "has-[:checked]:border-brand-500 has-[:checked]:bg-brand-50",
                )}
              >
                <input
                  type="radio"
                  name="publisherId"
                  value={publisher.id}
                  defaultChecked={publisher.publisherType === "Individual"}
                  className="mt-0.5"
                />
                <span>
                  <span className="block font-medium text-ink-900">{publisher.displayName}</span>
                  <span className="block text-xs text-ink-500">
                    {publisher.publisherType === "Agency" ? t("publisherAgency") : t("publisherIndividual")}
                  </span>
                </span>
              </label>
            ))}
          </div>
        </fieldset>
      )}

      {/* Only relevant before a listing has ever been reviewed — editing an existing listing
          doesn't re-explain the review flow to an owner who already went through it once. */}
      {!listing && (
        <div className="mt-5 flex items-center gap-2.5 rounded-xl border border-accent-100 bg-accent-100/40 px-4 py-3.5">
          <svg
            viewBox="0 0 24 24"
            className="h-[17px] w-[17px] shrink-0 text-accent-600"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
          >
            <path d="M12 3.5l7 2.6v5.2c0 5-3 8-7 9.2-4-1.2-7-4.2-7-9.2V6.1l7-2.6Z" strokeLinejoin="round" />
          </svg>
          <span className="text-[13px] text-ink-900">{t("draftNotice")}</span>
        </div>
      )}
    </div>
  );
}
