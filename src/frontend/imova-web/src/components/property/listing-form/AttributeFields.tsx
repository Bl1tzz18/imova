"use client";

import { useTranslations } from "next-intl";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { attributeInputName, type AttributeField } from "@/lib/property/attributeSchema";
import type { TypeSpecificAttributes } from "@/types/listing";

function asString(value: unknown): string | undefined {
  return typeof value === "number" || typeof value === "string" || typeof value === "boolean"
    ? String(value)
    : undefined;
}

// One input per TypeSpecificAttributes field of the selected PropertyType (see
// ATTRIBUTE_SCHEMA). Inputs are named "attr.<field>" so readAttributes() can rebuild the object
// on submit.
export function AttributeInput({
  field,
  initial,
  onChange,
}: {
  field: AttributeField;
  initial: TypeSpecificAttributes;
  // Lets a parent track a value other fields depend on (e.g. a House's heatingSystem).
  onChange?: (value: string) => void;
}) {
  const t = useTranslations("Attributes");
  const name = attributeInputName(field.name);
  const label = t(`${field.name}.label`);
  const defaultValue = asString(initial[field.name]);

  switch (field.kind) {
    case "int":
    case "decimal":
      return (
        <label className="block">
          <FieldLabel required={field.required}>{label}</FieldLabel>
          <TextInput
            name={name}
            type="number"
            min={field.min}
            max={field.max}
            step={field.kind === "int" ? "1" : "0.01"}
            defaultValue={defaultValue}
            required={field.required}
          />
          {t.has(`${field.name}.hint`) && <span className="mt-1 block text-xs text-ink-500">{t(`${field.name}.hint`)}</span>}
        </label>
      );
    case "enum":
      return (
        <label className="block">
          <FieldLabel required={field.required}>{label}</FieldLabel>
          <SelectInput
            name={name}
            defaultValue={defaultValue ?? ""}
            required={field.required}
            onChange={onChange ? (e) => onChange(e.target.value) : undefined}
          >
            <option value="">{field.required ? t("choose") : t("notSpecified")}</option>
            {field.options.map((option) => (
              <option key={option} value={option}>
                {t(`${field.name}.options.${option}`)}
              </option>
            ))}
          </SelectInput>
        </label>
      );
    case "yesno":
      return (
        <label className="block">
          <FieldLabel required={field.required}>{label}</FieldLabel>
          <SelectInput name={name} defaultValue={defaultValue ?? ""} required={field.required}>
            <option value="">{field.required ? t("choose") : t("notSpecified")}</option>
            <option value="true">{t("yes")}</option>
            <option value="false">{t("no")}</option>
          </SelectInput>
        </label>
      );
    case "text":
      return (
        <label className="block">
          <FieldLabel>{label}</FieldLabel>
          <TextInput
            name={name}
            maxLength={field.maxLength}
            defaultValue={defaultValue}
            placeholder={t.has(`${field.name}.placeholder`) ? t(`${field.name}.placeholder`) : undefined}
          />
        </label>
      );
    default:
      return null;
  }
}
