"use client";

import { useActionState, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { changePassword, type AuthFormState } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";
import { FieldLabel, TextInput } from "@/components/ui/Field";

const initialState: AuthFormState = {};

export function PasswordForm({ hasPassword }: { hasPassword: boolean }) {
  const tAuth = useTranslations("Auth");
  const tAccount = useTranslations("Account");
  const [state, formAction, pending] = useActionState(changePassword, initialState);
  const [mismatchError, setMismatchError] = useState<string | null>(null);

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    const form = e.currentTarget;
    const newPassword = (form.elements.namedItem("newPassword") as HTMLInputElement).value;
    const confirmPassword = (form.elements.namedItem("confirmPassword") as HTMLInputElement).value;

    if (newPassword !== confirmPassword) {
      e.preventDefault();
      setMismatchError(tAuth("passwordMismatch"));
      return;
    }
    setMismatchError(null);
  }

  return (
    <form action={formAction} onSubmit={handleSubmit} className="flex flex-col gap-3.5">
      {!hasPassword && (
        <p className="rounded-xl border border-ink-100 bg-ink-50 px-4 py-3 text-sm text-ink-600">
          {tAccount("setPasswordHint")}
        </p>
      )}

      {hasPassword && (
        <label className="block">
          <FieldLabel>{tAccount("currentPasswordLabel")}</FieldLabel>
          <TextInput type="password" name="currentPassword" required minLength={8} />
        </label>
      )}

      <label className="block">
        <FieldLabel>{tAccount("newPasswordLabel")}</FieldLabel>
        <TextInput type="password" name="newPassword" required minLength={8} />
      </label>

      <label className="block">
        <FieldLabel>{tAccount("confirmNewPasswordLabel")}</FieldLabel>
        <TextInput type="password" name="confirmPassword" required minLength={8} />
      </label>

      {(mismatchError ?? state.error) && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {mismatchError ?? state.error}
        </p>
      )}
      {state.success && (
        <p className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {tAccount("passwordUpdated")}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 self-start">
        {pending ? tAccount("updating") : hasPassword ? tAccount("updatePassword") : tAccount("setPassword")}
      </Button>
    </form>
  );
}
