"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";

// A top-bar filter: a pill that names the filter — or, once set, its value ("Apartament",
// "50 000–90 000 €") — and opens a small panel with the controls. Closes on a click outside, on
// Escape (focus back on the pill), or with "Gata"; the same click-outside/Escape pattern as
// SearchableSelect. Changes apply as they're made (see SearchNavigation), so there's no "Apply".
export function FilterPopover({
  label,
  value,
  onClear,
  align = "start",
  panelClassName,
  children,
}: {
  label: string;
  // The current value in words; null while the filter isn't set.
  value: string | null;
  onClear?: () => void;
  align?: "start" | "end";
  panelClassName?: string;
  children: (close: () => void) => ReactNode;
}) {
  const t = useTranslations("Search");
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const panelId = useId();

  useEffect(() => {
    if (!open) return;
    function onPointerDown(e: MouseEvent) {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    }
    function onKeyDown(e: KeyboardEvent) {
      if (e.key !== "Escape") return;
      setOpen(false);
      buttonRef.current?.focus();
    }
    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  const active = value !== null;
  return (
    <div ref={rootRef} className="relative">
      <button
        ref={buttonRef}
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        aria-label={active ? `${label}: ${value}` : label}
        onClick={() => setOpen((o) => !o)}
        className={cn(
          "flex h-10 max-w-[240px] items-center gap-2 rounded-full border px-4 text-sm font-medium transition-colors",
          active ? "border-brand-300 bg-brand-50 text-brand-800" : "border-line bg-white text-ink-800 hover:border-ink-300",
          open && "ring-2 ring-brand-500/20",
        )}
      >
        <span className="truncate">{value ?? label}</span>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className={cn("h-4 w-4 shrink-0 opacity-60 transition-transform", open && "rotate-180")} aria-hidden>
          <path d="m6 9 6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      {open && (
        <div
          id={panelId}
          role="dialog"
          aria-label={label}
          className={cn(
            "absolute top-full z-30 mt-2 rounded-2xl border border-line bg-white p-4 shadow-[0_12px_40px_-12px_rgba(15,23,42,0.25)]",
            align === "end" ? "right-0" : "left-0",
            panelClassName ?? "w-[340px]",
          )}
        >
          {children(() => setOpen(false))}
          <div className="mt-4 flex items-center justify-between border-t border-ink-100 pt-3">
            {onClear && active ? (
              <button type="button" onClick={onClear} className="text-sm font-medium text-ink-500 hover:text-ink-900">
                {t("clear")}
              </button>
            ) : (
              <span />
            )}
            <button type="button" onClick={() => setOpen(false)} className="rounded-full bg-ink-950 px-4 py-1.5 text-sm font-semibold text-white hover:bg-ink-800">
              {t("done")}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
