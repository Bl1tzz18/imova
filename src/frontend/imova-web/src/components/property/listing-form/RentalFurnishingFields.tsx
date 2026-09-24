"use client";

import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, SelectInput } from "@/components/ui/Field";
import { rentalFieldsForStep, rentalInputName } from "@/lib/property/rentalFields";
import type { RentalDetails } from "@/types/listing";

const FURNISHED_STATUSES = ["Unfurnished", "PartiallyFurnished", "Furnished"] as const;

// The rental terms that describe the home as offered (furnishing, pets) — asked on the "Details"
// step, only for a rental. They're still RentalDetails fields, not property attributes.
export function RentalFurnishingFields({
  transactionType,
  rental,
}: {
  transactionType: string;
  rental?: RentalDetails | null;
}) {
  const t = useTranslations("PropertyForm");
  const tFurnished = useTranslations("FurnishedStatus");
  const fields = rentalFieldsForStep("details", transactionType);
  if (fields.length === 0) return null;

  return (
    <section className="rounded-xl border border-ink-100 bg-white px-4 py-4">
      <h3 className="font-hero text-sm font-bold text-ink-950">{t("rentalFurnishingHeading")}</h3>
      <div className="mt-3 grid grid-cols-1 items-end gap-4 sm:grid-cols-2">
        {fields.includes("furnishedStatus") && (
          <label className="block">
            <FieldLabel>{t("furnishedStatusLabel")}</FieldLabel>
            <SelectInput
              name={rentalInputName("furnishedStatus")}
              defaultValue={rental?.furnishedStatus ?? "Unfurnished"}
            >
              {FURNISHED_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {tFurnished(status)}
                </option>
              ))}
            </SelectInput>
          </label>
        )}
        {fields.includes("petsAllowed") && (
          <Checkbox
            name={rentalInputName("petsAllowed")}
            value="true"
            defaultChecked={rental?.petsAllowed ?? false}
            className="pb-3 text-ink-700"
          >
            {t("petsAllowedLabel")}
          </Checkbox>
        )}
      </div>
    </section>
  );
}
