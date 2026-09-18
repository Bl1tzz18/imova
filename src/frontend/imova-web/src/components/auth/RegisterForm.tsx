"use client";

import { useActionState, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { register, type AuthFormState } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import { PhoneInput } from "@/components/ui/PhoneInput";

const initialState: AuthFormState = {};

export function RegisterForm({ next }: { next?: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(register, initialState);
  const [mismatchError, setMismatchError] = useState<string | null>(null);

  // The backend never receives confirmPassword — this is purely a client-side check that the two
  // fields match before letting the form submit at all.
  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    const form = e.currentTarget;
    const password = (form.elements.namedItem("password") as HTMLInputElement).value;
    const confirmPassword = (form.elements.namedItem("confirmPassword") as HTMLInputElement).value;

    if (password !== confirmPassword) {
      e.preventDefault();
      setMismatchError(t("passwordMismatch"));
      return;
    }
    setMismatchError(null);
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

      <label className="block">
        <FieldLabel>{t("passwordLabel")}</FieldLabel>
        <TextInput type="password" name="password" required minLength={8} />
      </label>

      <label className="block">
        <FieldLabel>{t("confirmPasswordLabel")}</FieldLabel>
        <TextInput type="password" name="confirmPassword" required minLength={8} />
      </label>

      <Checkbox name="acceptTerms" required>
        {t.rich("termsAgreement", {
          terms: (chunks) => <span className="font-medium text-ink-700">{chunks}</span>,
          privacy: (chunks) => <span className="font-medium text-ink-700">{chunks}</span>,
        })}
      </Checkbox>

      {(mismatchError ?? state.error) && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {mismatchError ?? state.error}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("registerPending") : t("registerSubmit")}
      </Button>
    </form>
  );
}
