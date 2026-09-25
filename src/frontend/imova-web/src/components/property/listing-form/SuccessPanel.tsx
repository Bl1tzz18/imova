"use client";

import { useTranslations } from "next-intl";
import { LinkButton } from "@/components/ui/Button";

export function SuccessPanel() {
  const t = useTranslations("PropertyForm");

  return (
    <div className="mt-10 flex flex-col items-center gap-4 rounded-2xl border border-ink-100 bg-white px-6 py-16 text-center shadow-[var(--shadow-card)]">
      <div className="flex h-15 w-15 items-center justify-center rounded-full bg-accent-100 text-accent-600">
        <svg
          viewBox="0 0 24 24"
          className="h-7 w-7"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M4.5 12.5 9.5 17.5 19.5 6.5" />
        </svg>
      </div>
      <h2 className="font-hero text-2xl font-bold text-ink-950">{t("successTitle")}</h2>
      <p className="max-w-md text-sm leading-relaxed text-ink-500">{t("successSubtitle")}</p>
      <div className="mt-2 flex flex-wrap justify-center gap-3">
        <LinkButton href="/my-listings">{t("successViewMine")}</LinkButton>
        <LinkButton href="/" variant="secondary">
          {t("successBackHome")}
        </LinkButton>
      </div>
    </div>
  );
}
