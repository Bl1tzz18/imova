"use client";

import { useActionState, useState } from "react";
import { useTranslations } from "next-intl";
import { Button, LinkButton } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Checkbox";
import { AuthNotice } from "@/components/auth/AuthCard";
import { confirmAccountDeletion, type AccountDeletionState } from "@/lib/account/actions";

const initialState: AccountDeletionState = {};

// The last step of the emailed deletion link: nothing is deleted until this is submitted.
export function ConfirmAccountDeletionForm({ userId, token }: { userId: string; token: string }) {
  const t = useTranslations("Account");
  const [acknowledged, setAcknowledged] = useState(false);
  const [state, formAction, pending] = useActionState(confirmAccountDeletion, initialState);

  // React resets the form after each submission — after an error, ask for the checkbox again.
  const [answered, setAnswered] = useState(state);
  if (answered !== state) {
    setAnswered(state);
    setAcknowledged(false);
  }

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="userId" value={userId} />
      <input type="hidden" name="token" value={token} />

      <Checkbox checked={acknowledged} onChange={(e) => setAcknowledged(e.target.checked)} className="text-ink-700">
        {t("deleteAcknowledge")}
      </Checkbox>

      {state.error && <AuthNotice tone="error">{state.error}</AuthNotice>}

      <Button type="submit" variant="danger" disabled={!acknowledged || pending} className="w-full">
        {pending ? t("deleting") : t("deleteConfirm")}
      </Button>
      <LinkButton href="/" variant="secondary" className="w-full">
        {t("deleteKeepAccount")}
      </LinkButton>
    </form>
  );
}
