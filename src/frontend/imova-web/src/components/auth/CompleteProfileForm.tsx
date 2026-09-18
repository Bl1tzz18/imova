"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { completePhoneNumber, type AuthFormState } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";
import { FieldLabel } from "@/components/ui/Field";
import { PhoneInput } from "@/components/ui/PhoneInput";

const initialState: AuthFormState = {};

export function CompleteProfileForm({ next }: { next?: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(completePhoneNumber, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3.5">
      <input type="hidden" name="next" value={next ?? ""} />
      <label className="block">
        <FieldLabel>{t("phoneLabel")}</FieldLabel>
        <PhoneInput name="phone" required />
      </label>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("registerPending") : t("completeProfileSubmit")}
      </Button>
    </form>
  );
}
