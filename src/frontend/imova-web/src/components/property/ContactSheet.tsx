"use client";

import { useRef, type ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

// "Date de contact" in the phone's bottom bar: opens the listing's contact card as a bottom sheet
// (a native <dialog> — focus stays inside, Escape and a tap outside close it). The card itself is
// server-rendered and passed in, so it's the same card as at the end of the page.
export function ContactSheet({
  label,
  closeLabel,
  primary,
  children,
}: {
  label: string;
  closeLabel: string;
  primary: boolean;
  children: ReactNode;
}) {
  const dialog = useRef<HTMLDialogElement>(null);

  return (
    <>
      <button
        type="button"
        onClick={() => dialog.current?.showModal()}
        className={cn(
          "inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-full px-5 text-sm font-medium transition-colors",
          primary
            ? "bg-accent-500 text-white shadow-sm shadow-accent-500/20 hover:bg-accent-600"
            : "border border-ink-200 bg-white text-ink-900 hover:border-ink-300 hover:bg-ink-50",
        )}
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden>
          <path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2Z" strokeLinejoin="round" />
        </svg>
        {label}
      </button>

      <dialog
        ref={dialog}
        aria-label={label}
        onClick={(e) => e.target === dialog.current && dialog.current.close()}
        className="mb-0 mt-auto max-h-[85dvh] w-full max-w-none rounded-t-2xl bg-transparent p-0 backdrop:bg-ink-950/50"
      >
        <div className="relative rounded-t-2xl bg-white pb-[max(0.5rem,env(safe-area-inset-bottom))] pt-3">
          {/* The grab handle is decoration; the × closes it. */}
          <span className="absolute left-1/2 top-2 h-1.5 w-10 -translate-x-1/2 rounded-full bg-ink-200" aria-hidden />
          <button
            type="button"
            onClick={() => dialog.current?.close()}
            aria-label={closeLabel}
            className="absolute right-3 top-4 z-10 flex h-9 w-9 items-center justify-center rounded-full text-ink-500 outline-none hover:bg-ink-50 hover:text-ink-900 focus-visible:ring-2 focus-visible:ring-brand-600/40"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4" aria-hidden>
              <path d="M6 6l12 12M18 6 6 18" strokeLinecap="round" />
            </svg>
          </button>
          {/* The page's own contact card, without its card frame (the sheet is the frame). */}
          <div className="[&>div]:border-0 [&>div]:shadow-none">{children}</div>
        </div>
      </dialog>
    </>
  );
}
