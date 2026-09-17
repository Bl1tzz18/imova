"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { login, type AuthFormState } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, TextInput } from "@/components/ui/Field";

const initialState: AuthFormState = {};

export function LoginForm({ next }: { next?: string }) {
  const t = useTranslations("Auth");
  const [state, formAction, pending] = useActionState(login, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3.5">
      <input type="hidden" name="next" value={next ?? ""} />

      <label className="block">
        <FieldLabel>{t("emailLabel")}</FieldLabel>
        <TextInput type="email" name="email" placeholder={t("emailPlaceholder")} required />
      </label>

      <label className="block">
        <FieldLabel>{t("passwordLabel")}</FieldLabel>
        <TextInput type="password" name="password" required minLength={8} />
      </label>

      <div className="flex items-center justify-between">
        <Checkbox name="rememberMe">{t("rememberMe")}</Checkbox>
        <span className="text-sm text-ink-400">{t("forgotPassword")}</span>
      </div>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 w-full">
        {pending ? t("loginPending") : t("loginSubmit")}
      </Button>
    </form>
  );
}
