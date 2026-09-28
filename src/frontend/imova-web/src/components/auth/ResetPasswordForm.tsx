"use client";

import { useActionState, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { resetPassword, type AuthFormState } from "@/lib/auth/actions";
import { AuthNotice } from "@/components/auth/AuthCard";
import { Button } from "@/components/ui/Button";
import { FieldLabel, TextInput } from "@/components/ui/Field";

const initialState: AuthFormState = {};

// email and token come from the emailed link; on success resetPassword() redirects to /login.
export function ResetPasswordForm({ email, token }: { email: string; token: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(resetPassword, initialState);
  const [mismatchError, setMismatchError] = useState<string | null>(null);

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    const form = e.currentTarget;
    const password = (form.elements.namedItem("newPassword") as HTMLInputElement).value;
    const confirmPassword = (form.elements.namedItem("confirmPassword") as HTMLInputElement).value;

    if (password !== confirmPassword) {
      e.preventDefault();
      setMismatchError(t("passwordMismatch"));
      return;
    }
    setMismatchError(null);
  }

  const error = mismatchError ?? state.error;

  return (
    <form action={formAction} onSubmit={handleSubmit} className="flex flex-col gap-3.5">
      <input type="hidden" name="email" value={email} />
      <input type="hidden" name="token" value={token} />

      <label className="block">
        <FieldLabel>{t("newPasswordLabel")}</FieldLabel>
        <TextInput type="password" name="newPassword" autoComplete="new-password" required minLength={8} />
      </label>

      <label className="block">
        <FieldLabel>{t("confirmPasswordLabel")}</FieldLabel>
        <TextInput type="password" name="confirmPassword" autoComplete="new-password" required minLength={8} />
      </label>

      {error && <AuthNotice tone="error">{error}</AuthNotice>}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("resetPasswordPending") : t("resetPasswordSubmit")}
      </Button>
    </form>
  );
}
