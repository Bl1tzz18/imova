"use client";

import { useTranslations } from "next-intl";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { attributeInputName, type AttributeField } from "@/lib/property/attributeSchema";
import type { TypeSpecificAttributes } from "@/types/listing";

function asString(value: unknown): string | undefined {
  return typeof value === "number" || typeof value === "string" || typeof value === "boolean"
    ? String(value)
    : undefined;
}

// One input per TypeSpecificAttributes field of the selected PropertyType (see
// ATTRIBUTE_SCHEMA). Scalar fields go in the caller's grid; bool/flags fields render as
// checkbox groups via <AttributeCheckboxes>. Inputs are named "attr.<field>" so
// readAttributes() can rebuild the object on submit.
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

export function AttributeCheckboxes({ field, initial }: { field: AttributeField; initial: TypeSpecificAttributes }) {
  const t = useTranslations("Attributes");

  if (field.kind === "bool") {
    return (
      <Checkbox
        name={attributeInputName(field.name)}
        value="true"
        defaultChecked={initial[field.name] === true}
        className="text-ink-700"
      >
        {t(`${field.name}.label`)}
      </Checkbox>
    );
  }

  if (field.kind !== "flags") return null;

  const current = (initial[field.name] ?? {}) as Record<string, unknown>;
  return (
    <fieldset>
      <legend className="mb-2 text-sm font-medium text-ink-700">{t(`${field.name}.label`)}</legend>
      <div className="flex flex-wrap gap-x-5 gap-y-2">
        {field.flags.map((flag) => (
          <Checkbox
            key={flag}
            name={attributeInputName(field.name, flag)}
            value="true"
            defaultChecked={current[flag] === true}
            className="text-ink-700"
          >
            {t(`utilityFlags.${flag}`)}
          </Checkbox>
        ))}
      </div>
    </fieldset>
  );
}
