"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { forgotPassword, type AuthFormState } from "@/lib/auth/actions";
import { AuthNotice } from "@/components/auth/AuthCard";
import { Button } from "@/components/ui/Button";
import { FieldLabel, TextInput } from "@/components/ui/Field";

const initialState: AuthFormState = {};

export function ForgotPasswordForm({ defaultEmail }: { defaultEmail?: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(forgotPassword, initialState);

  if (state.success) {
    return <AuthNotice tone="success">{t("forgotPasswordSent")}</AuthNotice>;
  }

  return (
    <form action={formAction} className="flex flex-col gap-3.5">
      <label className="block">
        <FieldLabel>{t("emailLabel")}</FieldLabel>
        <TextInput
          type="email"
          name="email"
          placeholder={t("emailPlaceholder")}
          defaultValue={defaultEmail}
          autoComplete="email"
          required
        />
      </label>

      {state.error && <AuthNotice tone="error">{state.error}</AuthNotice>}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("forgotPasswordPending") : t("forgotPasswordSubmit")}
      </Button>
    </form>
  );
}
