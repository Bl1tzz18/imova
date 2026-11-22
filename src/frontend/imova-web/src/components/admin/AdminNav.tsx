import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { getAdminSummary } from "@/lib/admin/summaryApi";
import { ADMIN_SECTIONS, adminSectionCount, badgeText, type AdminSectionKey } from "@/lib/admin/adminSections";
import { cn } from "@/lib/utils/cn";

// The admin area's own navigation, at the top of each admin page: one pill per section, the
// current one filled, with how many things wait there. Reached from the account menu's single
// "Administrare" entry, so admins move between sections here, not through that menu. Scrolls
// sideways on a phone.
export async function AdminNav({ current }: { current: AdminSectionKey }) {
  const [t, summary] = await Promise.all([getTranslations("Header"), getAdminSummary()]);

  return (
    <nav aria-label={t("administration")} className="mb-6">
      <p className="mb-2 flex items-center gap-1.5 text-xs font-bold uppercase tracking-[0.09em] text-accent-600">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
          <path d="M12 3 5 6v5c0 4.5 3 8.3 7 10 4-1.7 7-5.5 7-10V6Z" />
        </svg>
        {t("administration")}
      </p>
      <ul className="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 [scrollbar-width:none] sm:mx-0 sm:flex-wrap sm:px-0 [&::-webkit-scrollbar]:hidden">
        {ADMIN_SECTIONS.map((section) => {
          const count = adminSectionCount(summary, section.key);
          const active = section.key === current;
          return (
            <li key={section.key} className="shrink-0">
              <Link
                href={section.href}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "flex items-center gap-2 whitespace-nowrap rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors",
                  active
                    ? "border-ink-900 bg-ink-900 text-white"
                    : "border-ink-200 bg-white text-ink-700 hover:border-ink-300 hover:bg-ink-50",
                )}
              >
                {t(section.label)}
                {count > 0 && (
                  <span
                    className={cn(
                      "rounded-full px-1.5 text-[11px] font-bold leading-[18px]",
                      active ? "bg-white text-ink-900" : "bg-accent-500 text-white",
                    )}
                  >
                    {badgeText(count)}
                  </span>
                )}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
