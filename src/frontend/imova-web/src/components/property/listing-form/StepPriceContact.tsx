"use client";

import { useTranslations } from "next-intl";
import { FieldLabel, PriceInput, SelectInput } from "@/components/ui/Field";
import type { Property } from "@/types/property";

// Mirrors Imova.Domain.Properties.SupportedCurrencies on the backend, which is the server-side
// source of truth — the backend still validates independently. Keep the two in sync.
const SUPPORTED_CURRENCIES = ["EUR", "MDL", "USD"] as const;
const DEFAULT_CURRENCY = "EUR";

export function StepPriceContact({ property }: { property?: Property }) {
  const t = useTranslations("PropertyForm");

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step4Heading")}</h2>

      <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label className="block">
          <FieldLabel required>{t("priceLabel")}</FieldLabel>
          <PriceInput name="price" required defaultValue={property?.price} />
        </label>
        <label className="block">
          <FieldLabel required>{t("currencyLabel")}</FieldLabel>
          <SelectInput name="currency" required defaultValue={property?.currency ?? DEFAULT_CURRENCY}>
            {SUPPORTED_CURRENCIES.map((currency) => (
              <option key={currency} value={currency}>
                {currency}
              </option>
            ))}
          </SelectInput>
        </label>
      </div>

      {/* Only relevant before a listing has ever been reviewed — editing an existing listing
          doesn't re-explain the review flow to an owner who already went through it once. */}
      {!property && (
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
