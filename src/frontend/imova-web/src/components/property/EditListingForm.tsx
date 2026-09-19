"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { FieldLabel, PriceInput, TextAreaInput, TextInput } from "@/components/ui/Field";
import { updatePropertyDetails, type UpdatePropertyState } from "@/lib/property/actions";
import type { Property } from "@/types/property";

const initialState: UpdatePropertyState = {};

export function EditListingForm({ property }: { property: Property }) {
  const tForm = useTranslations("PropertyForm");
  const tEdit = useTranslations("EditListingPage");
  const boundAction = updatePropertyDetails.bind(null, property.id);
  const [state, formAction, pending] = useActionState(boundAction, initialState);
  const isRejected = property.status === "Rejected";

  return (
    <form action={formAction} className="flex flex-col gap-4">
      {isRejected && (
        <div className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          <p className="font-medium">{tEdit("rejectedNoticeTitle")}</p>
          {property.rejectionReason && <p className="mt-0.5">{property.rejectionReason}</p>}
          <p className="mt-1.5">{tEdit("rejectedNoticeBody")}</p>
        </div>
      )}

      <label className="block">
        <FieldLabel required>{tForm("titleLabel")}</FieldLabel>
        <TextInput
          type="text"
          name="title"
          required
          maxLength={200}
          defaultValue={property.title}
          placeholder={tForm("titlePlaceholder")}
        />
      </label>

      <label className="block">
        <FieldLabel required>{tForm("descriptionLabel")}</FieldLabel>
        <TextAreaInput
          name="description"
          required
          maxLength={4000}
          rows={6}
          defaultValue={property.description}
          placeholder={tForm("descriptionPlaceholder")}
        />
      </label>

      <label className="block max-w-xs">
        <FieldLabel required>{tForm("priceLabel")}</FieldLabel>
        <PriceInput name="price" required defaultValue={property.price} />
      </label>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}
      {state.success && (
        <p className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {isRejected ? tEdit("savedAndResubmitted") : tEdit("saved")}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 self-start">
        {pending ? tEdit("saving") : isRejected ? tEdit("saveAndResubmit") : tEdit("saveChanges")}
      </Button>
    </form>
  );
}
