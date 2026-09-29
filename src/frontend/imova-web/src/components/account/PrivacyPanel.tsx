"use client";

import { useTranslations } from "next-intl";
import { buttonClassName } from "@/components/ui/Button";
import { DeleteAccountSection } from "@/components/account/DeleteAccountSection";
import type { AccountDataSummary } from "@/lib/account/deletion";

export type ExportStatus = "tooSoon" | "failed";

// The "My data" tab of /account: the user's data-protection rights in one place — a copy of
// everything we store (GDPR art. 15/20), correcting it (the Profile tab), and erasing the account.
export function PrivacyPanel({
  email,
  hasPassword,
  summary,
  exportStatus,
}: {
  email: string;
  hasPassword: boolean;
  summary: AccountDataSummary | null;
  exportStatus?: ExportStatus;
}) {
  const t = useTranslations("Account");

  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-ink-950">{t("privacyTitle")}</h2>
        <p className="text-sm text-ink-500">{t("privacyBody")}</p>

        {exportStatus && (
          <p role="alert" className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
            {exportStatus === "tooSoon" ? t("exportTooSoon") : t("exportFailed")}
          </p>
        )}

        {/* A plain <a> to the route handler: next/link would prefetch it (and build an export). */}
        <a href="/account/data-export" className={buttonClassName({ variant: "secondary", className: "mt-1 self-start" })}>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden>
            <path d="M12 4v11m0 0-4-4m4 4 4-4M5 19h14" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          {t("downloadData")}
        </a>
        <p className="text-xs text-ink-400">{t("downloadHint")}</p>
        <p className="text-sm text-ink-500">{t("rectifyHint")}</p>
      </section>

      <DeleteAccountSection email={email} hasPassword={hasPassword} summary={summary} />

      <p className="border-t border-ink-100 pt-5 text-xs leading-relaxed text-ink-400">{t("privacyRights")}</p>
    </div>
  );
}
