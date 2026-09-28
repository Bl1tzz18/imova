"use client";

import { useActionState, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { resetPassword, type AuthFormState } from "@/lib/auth/actions";
import { AuthNotice } from "@/components/auth/AuthCard";
import { Button } from "@/components/ui/Button";
import { PasswordFields } from "@/components/auth/PasswordFields";
import { confirmationState, meetsPasswordRules } from "@/lib/auth/passwordRules";

const initialState: AuthFormState = {};

// email and token come from the emailed link; on success resetPassword() redirects to /login.
export function ResetPasswordForm({ email, token }: { email: string; token: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(resetPassword, initialState);
  const [attempted, setAttempted] = useState(false);

  // Only submit once the password meets the policy and the confirmation matches — otherwise
  // PasswordFields turns whatever is still missing red.
  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    const form = e.currentTarget;
    const password = (form.elements.namedItem("newPassword") as HTMLInputElement).value;
    const confirmation = (form.elements.namedItem("confirmPassword") as HTMLInputElement).value;

    if (!meetsPasswordRules(password) || confirmationState(password, confirmation) !== "match") {
      e.preventDefault();
      setAttempted(true);
    }
  }

  return (
    <form action={formAction} onSubmit={handleSubmit} className="flex flex-col gap-3.5">
      <input type="hidden" name="email" value={email} />
      <input type="hidden" name="token" value={token} />

      <PasswordFields
        passwordName="newPassword"
        passwordLabel={t("newPasswordLabel")}
        confirmLabel={t("confirmPasswordLabel")}
        attempted={attempted}
      />

      {state.error && <AuthNotice tone="error">{state.error}</AuthNotice>}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("resetPasswordPending") : t("resetPasswordSubmit")}
      </Button>
    </form>
  );
}
