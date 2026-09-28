"use client";

import { useActionState, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { register, type AuthFormState } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import { PhoneInput } from "@/components/ui/PhoneInput";
import { PasswordFields } from "@/components/auth/PasswordFields";
import { confirmationState, meetsPasswordRules } from "@/lib/auth/passwordRules";

const initialState: AuthFormState = {};

export function RegisterForm({ next }: { next?: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(register, initialState);
  const [attempted, setAttempted] = useState(false);

  // Only submit once the password meets the policy and the confirmation matches — otherwise
  // PasswordFields turns whatever is still missing red.
  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    const form = e.currentTarget;
    const password = (form.elements.namedItem("password") as HTMLInputElement).value;
    const confirmation = (form.elements.namedItem("confirmPassword") as HTMLInputElement).value;

    if (!meetsPasswordRules(password) || confirmationState(password, confirmation) !== "match") {
      e.preventDefault();
      setAttempted(true);
    }
  }

  return (
    <form action={formAction} onSubmit={handleSubmit} className="flex flex-col gap-3.5">
      <input type="hidden" name="next" value={next ?? ""} />
      <label className="block">
        <FieldLabel>{t("nameLabel")}</FieldLabel>
        <TextInput type="text" name="name" placeholder={t("namePlaceholder")} required />
      </label>

      <label className="block">
        <FieldLabel>{t("emailLabel")}</FieldLabel>
        <TextInput type="email" name="email" placeholder={t("emailPlaceholder")} required />
      </label>

      <label className="block">
        <FieldLabel>{t("phoneLabel")}</FieldLabel>
        <PhoneInput name="phone" required />
      </label>

      <PasswordFields
        passwordName="password"
        passwordLabel={t("passwordLabel")}
        confirmLabel={t("confirmPasswordLabel")}
        attempted={attempted}
      />

      <Checkbox name="acceptTerms" required>
        {t.rich("termsAgreement", {
          terms: (chunks) => <span className="font-medium text-ink-700">{chunks}</span>,
          privacy: (chunks) => <span className="font-medium text-ink-700">{chunks}</span>,
        })}
      </Checkbox>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("registerPending") : t("registerSubmit")}
      </Button>
    </form>
  );
}
