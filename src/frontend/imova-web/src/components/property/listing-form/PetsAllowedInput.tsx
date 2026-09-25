"use client";

import { useTranslations } from "next-intl";
import { FieldLabel, SelectInput } from "@/components/ui/Field";
import { rentalInputName } from "@/lib/property/rentalFields";

// The rental "pets allowed" answer — a required Yes/No dropdown. Posted as
// rental.petsAllowed into the listing's RentalDetails.
export function PetsAllowedInput({ defaultValue }: { defaultValue?: boolean | null }) {
  const t = useTranslations("PropertyForm");
  const tAttr = useTranslations("Attributes");

  return (
    <label className="block">
      <FieldLabel required>{t("petsAllowedLabel")}</FieldLabel>
      <SelectInput
        name={rentalInputName("petsAllowed")}
        required
        defaultValue={defaultValue == null ? "" : String(defaultValue)}
      >
        <option value="">{tAttr("choose")}</option>
        <option value="true">{tAttr("yes")}</option>
        <option value="false">{tAttr("no")}</option>
      </SelectInput>
    </label>
  );
}
