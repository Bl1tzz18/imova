"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import { AGENCY_SECTIONS, agencyManagePath, sectionOf } from "@/lib/agency/manage";
import { cn } from "@/lib/utils/cn";

// Profil · Membri · Anunțuri · Setări — each its own address, so a tab can be linked to and the
// listings' own ?tab= stays theirs. Four equal segments, which fit side by side down to 360 px.
export function AgencyTabs({ agencyId }: { agencyId: string }) {
  const t = useTranslations("AgencyManage");
  const current = sectionOf(usePathname());

  return (
    <nav aria-label={t("sectionsLabel")} className="grid grid-cols-4 gap-1 rounded-2xl bg-ink-100/70 p-1 sm:max-w-xl">
      {AGENCY_SECTIONS.map((section) => (
        <Link
          key={section}
          href={agencyManagePath(agencyId, section)}
          aria-current={current === section ? "page" : undefined}
          className={cn(
            "flex min-h-11 items-center justify-center rounded-xl px-1.5 text-center text-sm font-medium transition-colors",
            current === section ? "bg-white text-ink-950 shadow-sm" : "text-ink-600 hover:text-ink-900",
          )}
        >
          {t(`section.${section}`)}
        </Link>
      ))}
    </nav>
  );
}
