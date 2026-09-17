"use client";

import { useTranslations } from "next-intl";
import { FieldLabel, PriceInput, TextInput } from "@/components/ui/Field";

export function StepPriceContact({ ownerId }: { ownerId: string }) {
  const t = useTranslations("PropertyForm");

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step4Heading")}</h2>

      <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label className="block">
          <FieldLabel required>{t("priceLabel")}</FieldLabel>
          <PriceInput name="price" required />
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

      <label className="mt-4 block">
        <FieldLabel required>{t("ownerIdLabel")}</FieldLabel>
        <TextInput name="ownerId" required defaultValue={ownerId} />
      </label>

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
    </div>
  );
}
