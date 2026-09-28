"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { signOutOtherSessions, type AuthFormState } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";

const initialState: AuthFormState = {};

// Under the password form in the Security tab: ends every other session of this account (other
// browsers, a lost phone) while this one stays signed in.
export function OtherSessionsPanel() {
  const t = useTranslations("Account");
  const [state, formAction, pending] = useActionState(signOutOtherSessions, initialState);

  return (
    <form action={formAction} className="mt-8 flex flex-col gap-3 border-t border-ink-100 pt-7">
      <h2 className="text-base font-semibold text-ink-950">{t("sessionsTitle")}</h2>
      <p className="text-sm text-ink-500">{t("sessionsBody")}</p>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}
      {state.success && (
        <p className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {t("otherSessionsSignedOut")}
        </p>
      )}

      <Button type="submit" variant="secondary" disabled={pending} className="mt-1 self-start">
        {pending ? t("signingOut") : t("signOutOtherSessions")}
      </Button>
    </form>
  );
}
