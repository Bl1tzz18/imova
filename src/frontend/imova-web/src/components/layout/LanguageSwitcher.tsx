"use client";

import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useTransition } from "react";
import { locales, type Locale } from "@/i18n/config";
import { setLocale } from "@/i18n/actions";
import { cn } from "@/lib/utils/cn";

export function LanguageSwitcher() {
  const locale = useLocale();
  const t = useTranslations("Common");
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  function handleSelect(next: Locale) {
    if (next === locale) return;
    startTransition(async () => {
      await setLocale(next);
      router.refresh();
    });
  }

  // On a phone the three buttons don't fit beside the logo and "Adaugă anunț": a compact native
  // select instead ("RO ▾", the phone's own picker). From sm up, the buttons.
  return (
    <>
      <select
        value={locale}
        onChange={(e) => handleSelect(e.target.value as Locale)}
        disabled={isPending}
        aria-label={t("selectLanguage")}
        className="h-9 shrink-0 rounded-full border border-ink-200 bg-white pl-2.5 pr-1 text-xs font-semibold uppercase tracking-wide text-ink-900 disabled:opacity-50 sm:hidden"
      >
        {locales.map((l) => (
          <option key={l} value={l}>
            {l.toUpperCase()}
          </option>
        ))}
      </select>
      <div
        role="group"
        aria-label={t("selectLanguage")}
        className="hidden items-center gap-0.5 rounded-full border border-ink-200 bg-white p-0.5 text-xs font-semibold sm:flex"
      >
        {locales.map((l) => (
          <button
            key={l}
            type="button"
            onClick={() => handleSelect(l)}
            disabled={isPending}
            aria-current={l === locale}
            className={cn(
              "rounded-full px-2.5 py-1 uppercase tracking-wide transition-colors disabled:opacity-50",
              l === locale
                ? "bg-ink-900 text-white"
                : "text-ink-500 hover:bg-ink-50 hover:text-ink-900",
            )}
          >
            {l}
          </button>
        ))}
      </div>
    </>
  );
}
