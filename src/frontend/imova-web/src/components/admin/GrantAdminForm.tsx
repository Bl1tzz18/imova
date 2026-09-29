"use client";

import { useActionState, useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import { grantAdmin, type GrantAdminState } from "@/lib/admin/actions";

const initialState: GrantAdminState = {};

// Adding an admin: the person's account email, the acting admin's own password, and an explicit
// acknowledgement — the API does the actual checks (see GrantAdminHandler).
export function GrantAdminForm() {
  const t = useTranslations("AdminAdminsPage");
  const [state, formAction, pending] = useActionState(grantAdmin, initialState);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [acknowledged, setAcknowledged] = useState(false);

  // React resets the form after each submission: start over whenever an answer comes back.
  const [answered, setAnswered] = useState(state);
  if (answered !== state) {
    setAnswered(state);
    setPassword("");
    setAcknowledged(false);
    if (state.grantedEmail) setEmail("");
  }

  const ready = email.trim().length > 0 && password.length > 0 && acknowledged;

  return (
    <form action={formAction} className="flex flex-col gap-3.5">
      <label className="block">
        <FieldLabel>{t("emailLabel")}</FieldLabel>
        <TextInput
          type="email"
          name="email"
          required
          autoComplete="off"
          maxLength={256}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="nume@exemplu.com"
        />
      </label>
      <label className="block">
        <FieldLabel>{t("passwordLabel")}</FieldLabel>
        <TextInput
          type="password"
          name="password"
          required
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
      </label>
      <Checkbox checked={acknowledged} onChange={(e) => setAcknowledged(e.target.checked)} className="text-ink-700">
        {t("acknowledge")}
      </Checkbox>

      {state.error && (
        <p role="alert" className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}
      {state.grantedEmail && (
        <p role="status" className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {t("granted", { email: state.grantedEmail })}
        </p>
      )}

      <Button type="submit" disabled={!ready || pending} className="self-start">
        {pending ? t("granting") : t("grant")}
      </Button>
    </form>
  );
}
