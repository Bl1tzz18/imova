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

  return (
    <div
      role="group"
      aria-label={t("selectLanguage")}
      className="flex items-center gap-0.5 rounded-full border border-ink-200 bg-white p-0.5 text-xs font-semibold"
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
  );
}
