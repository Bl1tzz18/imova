"use client";

import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { clearAllFilters, clearDetailFilters } from "@/lib/search/filters";
import { cn } from "@/lib/utils/cn";
import { SearchFilters } from "./SearchFilters";
import { useSearchNavigation } from "./SearchNavigation";

// The filters that don't fit in the top bar. variant "side" (desktop): a panel sliding over the
// right edge with the type's detailed filters — the bar keeps transaction, type, location and
// price. variant "sheet" (phone): full screen with every filter, the only filter UI there. Either
// way the results update behind it as filters change, and the footer says how many there are.
// Rendered into <body>: its trigger sits in the sticky, backdrop-blurred top bar, and a
// backdrop-filter makes an element the containing block of its position:fixed descendants — the
// drawer would be clipped to the bar instead of covering the page.
export function FiltersDrawer({
  open,
  onClose,
  variant,
  totalCount,
}: {
  open: boolean;
  onClose: () => void;
  variant: "side" | "sheet";
  totalCount: number;
}) {
  const t = useTranslations("Search");
  const { state, change, pending } = useSearchNavigation();
  const closeRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    closeRef.current?.focus();
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    return () => {
      document.body.style.overflow = previous;
      document.removeEventListener("keydown", onKey);
    };
  }, [open, onClose]);

  if (!open) return null;

  const side = variant === "side";
  // The side panel only clears its own filters — the bar's stay (they're visible, and one click
  // away from being changed there).
  const clearDetails = () => change(clearDetailFilters(state));
  const title = side ? t("moreFilters") : t("filters");

  return createPortal(
    <div className={cn("fixed inset-0 z-50 flex", side ? "hidden justify-end lg:flex" : "lg:hidden")}>
      {side && <div className="absolute inset-0 bg-ink-950/30" onClick={onClose} aria-hidden />}
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={cn("relative flex h-full w-full flex-col bg-ink-50", side && "max-w-[480px] shadow-2xl")}
      >
        <div className="flex items-center justify-between border-b border-line bg-white px-5 py-4">
          <span className="font-hero text-lg font-bold text-ink-950">{title}</span>
          <div className="flex items-center gap-3">
            {pending && <span className="text-xs text-ink-400">{t("updating")}</span>}
            <button ref={closeRef} type="button" onClick={onClose} aria-label={t("close")} className="flex h-9 w-9 items-center justify-center rounded-full text-ink-500 hover:bg-bubble">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-5 w-5" aria-hidden>
                <path d="M6 6l12 12M18 6 6 18" strokeLinecap="round" />
              </svg>
            </button>
          </div>
        </div>
        <div className="flex-1 overflow-y-auto px-4 py-4 sm:px-5">
          <SearchFilters scope={side ? "details" : "all"} idPrefix={variant} />
        </div>
        <div className="flex items-center gap-3 border-t border-line bg-white p-4">
          {side ? (
            <button type="button" onClick={clearDetails} className="text-sm font-medium text-ink-600 hover:text-ink-950">
              {t("clearDetails")}
            </button>
          ) : (
            <button type="button" onClick={() => change(clearAllFilters(state))} className="text-sm font-medium text-ink-600 hover:text-ink-950">
              {t("reset")}
            </button>
          )}
          <button type="button" onClick={onClose} className="h-12 flex-1 rounded-full bg-accent-500 text-sm font-semibold text-white hover:bg-accent-600">
            {t("showResults", { count: totalCount })}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
