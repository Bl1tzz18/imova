"use client";

import { useLocale, useTranslations } from "next-intl";
import { usePathname, useRouter } from "@/i18n/navigation";
import { useTransition } from "react";
import { LOCALE_MENU_ORDER, LOCALE_NAMES, type Locale } from "@/i18n/config";
import { setLocale } from "@/i18n/actions";
import { DropdownMenu } from "@/components/ui/DropdownMenu";

// The header's language control, the same on a phone and a desktop: a globe with the current code
// ("🌐 RO ▾") opening a short menu of the languages, each named in its own language. People pick a
// language once (or get it from the browser/account), so it stays compact instead of three buttons.
// `quiet` (desktop header): no border, like the icons beside it — a site setting, not a button to
// press; on a phone it keeps its border to match the search field next to it.
export function LanguageSwitcher({ quiet = false }: { quiet?: boolean }) {
  const locale = useLocale() as Locale;
  const t = useTranslations("Common");
  const router = useRouter();
  const pathname = usePathname();
  const [isPending, startTransition] = useTransition();

  function handleSelect(next: Locale) {
    if (next === locale) return;
    startTransition(async () => {
      // The cookie (and a signed-in account's language) first, then the same page at the other
      // language's address: /search?… ↔ /ru/search?….
      await setLocale(next);
      // (The query is read at click time — useSearchParams would need a Suspense boundary.)
      router.replace(`${pathname}${window.location.search}`, { locale: next });
    });
  }

  return (
    <DropdownMenu
      label={`${t("selectLanguage")}: ${LOCALE_NAMES[locale]}`}
      disabled={isPending}
      menuMinWidth="min-w-44"
      triggerClassName={
        quiet
          ? "flex h-10 items-center gap-1.5 rounded-full px-3 text-xs font-semibold uppercase tracking-wide text-ink-600 transition-colors hover:bg-ink-100 hover:text-ink-950 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 disabled:opacity-50 aria-expanded:bg-ink-100 aria-expanded:text-ink-950"
          : "flex h-9 items-center gap-1.5 rounded-full border border-ink-200 bg-white pl-2.5 pr-2 text-xs font-semibold uppercase tracking-wide text-ink-900 transition-colors hover:border-ink-300 hover:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 disabled:opacity-50 aria-expanded:border-ink-300 aria-expanded:bg-ink-50"
      }
      trigger={
        <>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" className="h-4 w-4" aria-hidden>
            <circle cx="12" cy="12" r="9" />
            <path d="M3 12h18M12 3c2.5 2.6 3.8 5.6 3.8 9s-1.3 6.4-3.8 9c-2.5-2.6-3.8-5.6-3.8-9S9.5 5.6 12 3Z" />
          </svg>
          {locale}
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="h-3.5 w-3.5 opacity-70" aria-hidden>
            <path d="m6 9 6 6 6-6" />
          </svg>
        </>
      }
      items={LOCALE_MENU_ORDER.map((l) => ({
        id: l,
        label: LOCALE_NAMES[l],
        hint: l.toUpperCase(),
        checked: l === locale,
        onSelect: () => handleSelect(l),
      }))}
    />
  );
}
