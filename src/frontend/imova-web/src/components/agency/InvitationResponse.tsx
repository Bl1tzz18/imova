"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { AuthNotice } from "@/components/auth/AuthCard";
import { Button, LinkButton } from "@/components/ui/Button";
import { respondToInvitation, type InvitationResponseState } from "@/lib/agency/actions";

const initialState: InvitationResponseState = {};

// Accept / Decline on an invitation addressed to the signed-in account; then what happened.
export function InvitationResponse({ token, agencyName }: { token: string; agencyName: string }) {
  const t = useTranslations("Invitation");
  const [state, formAction, pending] = useActionState(respondToInvitation, initialState);

  if (state.outcome === "accepted") {
    return (
      <div className="flex flex-col gap-4">
        <AuthNotice tone="success">{t("accepted", { agency: agencyName })}</AuthNotice>
        <LinkButton href="/account" className="w-full">
          {t("goToAccount")}
        </LinkButton>
      </div>
    );
  }

  if (state.outcome === "declined") {
    return (
      <div className="flex flex-col gap-4">
        <AuthNotice tone="success">{t("declined")}</AuthNotice>
        <LinkButton href="/" variant="secondary" className="w-full">
          {t("goHome")}
        </LinkButton>
      </div>
    );
  }

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <input type="hidden" name="token" value={token} />
      {state.error && <AuthNotice tone="error">{state.error}</AuthNotice>}
      <Button type="submit" name="answer" value="accept" disabled={pending} className="w-full">
        {t("accept")}
      </Button>
      <Button type="submit" name="answer" value="decline" variant="secondary" disabled={pending} className="w-full">
        {t("decline")}
      </Button>
    </form>
  );
}
