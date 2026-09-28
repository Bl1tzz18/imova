"use client";

import { useState, useTransition } from "react";
import { useTranslations } from "next-intl";
import { resendEmailConfirmation } from "@/lib/auth/actions";
import { Button } from "@/components/ui/Button";

// Shown to a signed-in user whose email isn't confirmed yet (UserProfile.emailConfirmed): their
// listings wait as drafts and only go to review once the emailed link is opened.
export function EmailConfirmationBanner({ email, className }: { email: string; className?: string }) {
  const t = useTranslations("Auth");
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<{ sent?: boolean; error?: string }>({});

  function resend() {
    startTransition(async () => {
      const { success, error } = await resendEmailConfirmation();
      setResult(success ? { sent: true } : { error });
    });
  }

  return (
    <div
      role="status"
      className={`flex flex-col gap-3 rounded-2xl border border-brand-200 bg-brand-50 px-5 py-4 sm:flex-row sm:items-center sm:justify-between ${className ?? ""}`}
    >
      <div className="text-sm text-brand-900">
        <p className="font-medium">{t("confirmBannerTitle", { email })}</p>
        <p className="mt-0.5 text-brand-800">
          {result.sent ? t("confirmBannerSent") : (result.error ?? t("confirmBannerBody"))}
        </p>
      </div>
      {!result.sent && (
        <Button type="button" variant="secondary" size="sm" onClick={resend} disabled={pending} className="shrink-0">
          {pending ? t("confirmBannerResending") : t("confirmBannerResend")}
        </Button>
      )}
    </div>
  );
}
