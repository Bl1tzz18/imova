"use client";

import { useRef, type ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

// A collapsible card with an icon, a title and an optional status (a check mark, a count) — the
// listing form's details step and the /search filter panel. The body stays mounted while collapsed
// (only hidden), so form inputs inside it are still submitted and validated.
//
// sticky: while open, the header sticks to the top of the scrolling area (top: its offset there,
// e.g. below the site header) until the section ends, so collapsing a long section never means
// scrolling back up to it. Collapsing from a stuck header scrolls the card back into view
// (scrollMargin: the room to leave above it) — otherwise the list would jump to wherever the
// now-shorter content ends. Both are Tailwind classes, e.g. { top: "top-0", scrollMargin: "scroll-mt-3" }.
export function AccordionSection({
  id,
  icon,
  title,
  status,
  open,
  onToggle,
  onInvalidCapture,
  sticky,
  children,
}: {
  id: string;
  // The <path>s of a 24×24 stroke icon.
  icon: ReactNode;
  title: string;
  status?: ReactNode;
  open: boolean;
  onToggle: () => void;
  onInvalidCapture?: () => void;
  sticky?: { top: string; scrollMargin: string };
  children: ReactNode;
}) {
  const panelId = `accordion-section-${id}`;
  const cardRef = useRef<HTMLDivElement>(null);
  const headerRef = useRef<HTMLButtonElement>(null);

  function toggle() {
    const card = cardRef.current;
    const header = headerRef.current;
    // Stuck = the card's top has scrolled past its header.
    const collapsingFromStuckHeader =
      open && sticky !== undefined && card !== null && header !== null && header.getBoundingClientRect().top - card.getBoundingClientRect().top > 1;
    onToggle();
    if (collapsingFromStuckHeader) requestAnimationFrame(() => card.scrollIntoView({ block: "start" }));
  }

  return (
    <div ref={cardRef} className={cn("rounded-xl border bg-white", open ? "border-ink-200" : "border-ink-100", sticky?.scrollMargin)}>
      <button
        ref={headerRef}
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        onClick={toggle}
        className={cn(
          // White so content scrolling under a stuck header doesn't show through. Rounded like the
          // card's inside (12px - 1px border) — all corners while it's the whole card (collapsed),
          // or its square corners would cover the card's rounded border.
          "flex w-full items-center gap-3 bg-white px-4 py-3 text-left",
          open ? "rounded-t-[11px]" : "rounded-[11px]",
          // The divider lives on the header (not the panel) so a stuck header keeps its edge.
          open && "border-b border-ink-100",
          open && sticky && ["sticky z-10", sticky.top],
        )}
      >
        <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-700">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" className="h-[18px] w-[18px]" aria-hidden>
            {icon}
          </svg>
        </span>
        <span className="flex-1 font-hero text-sm font-bold text-ink-950">{title}</span>
        {status}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className={cn("h-4 w-4 text-ink-400 transition-transform", open && "rotate-180")} aria-hidden>
          <path d="m6 9 6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      <div id={panelId} className={cn("px-4 py-4", !open && "hidden")} onInvalidCapture={onInvalidCapture}>
        {children}
      </div>
    </div>
  );
}
