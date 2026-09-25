"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { activeFilterCount } from "@/lib/search/filters";
import { SearchFilters } from "./SearchFilters";
import { useSearchNavigation } from "./SearchNavigation";

// Mobile: a "Filtre" button opening the filter panel full-screen; the results update behind it as
// filters change, and the footer button shows how many there are.
export function FiltersSheet({ totalCount }: { totalCount: number }) {
  const t = useTranslations("Search");
  const { state } = useSearchNavigation();
  const [open, setOpen] = useState(false);
  const count = activeFilterCount(state);

  useEffect(() => {
    if (!open) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && setOpen(false);
    document.addEventListener("keydown", onKey);
    return () => {
      document.body.style.overflow = previous;
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex h-10 items-center gap-2 rounded-full border border-line bg-white px-4 text-sm font-medium text-ink-800 lg:hidden"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden>
          <path d="M4 6h16M7 12h10M10 18h4" strokeLinecap="round" />
        </svg>
        {t("filters")}
        {count > 0 && <span className="rounded-full bg-accent-500 px-1.5 text-xs font-semibold text-white">{count}</span>}
      </button>

      {open && (
        <div role="dialog" aria-modal="true" aria-label={t("filters")} className="fixed inset-0 z-50 flex flex-col bg-white lg:hidden">
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <span className="font-hero text-lg font-bold text-ink-950">{t("filters")}</span>
            <button type="button" onClick={() => setOpen(false)} aria-label={t("close")} className="flex h-9 w-9 items-center justify-center rounded-full text-ink-500 hover:bg-bubble">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-5 w-5" aria-hidden>
                <path d="M6 6l12 12M18 6 6 18" strokeLinecap="round" />
              </svg>
            </button>
          </div>
          <div className="flex-1 overflow-y-auto px-4 py-4">
            <SearchFilters inSheet />
          </div>
          <div className="border-t border-line p-4">
            <button type="button" onClick={() => setOpen(false)} className="h-12 w-full rounded-full bg-accent-500 text-sm font-semibold text-white hover:bg-accent-600">
              {t("showResults", { count: totalCount })}
            </button>
          </div>
        </div>
      )}
    </>
  );
}
