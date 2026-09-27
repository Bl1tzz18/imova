import type { ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

// A collapsible card with an icon, a title and an optional status (a check mark, a count) — the
// listing form's details step and the /search filter panel. The body stays mounted while collapsed
// (only hidden), so form inputs inside it are still submitted and validated.
export function AccordionSection({
  id,
  icon,
  title,
  status,
  open,
  onToggle,
  onInvalidCapture,
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
  children: ReactNode;
}) {
  const panelId = `accordion-section-${id}`;
  return (
    <div className={cn("rounded-xl border bg-white", open ? "border-ink-200" : "border-ink-100")}>
      <button type="button" aria-expanded={open} aria-controls={panelId} onClick={onToggle} className="flex w-full items-center gap-3 px-4 py-3 text-left">
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
      <div id={panelId} className={cn("border-t border-ink-100 px-4 py-4", !open && "hidden")} onInvalidCapture={onInvalidCapture}>
        {children}
      </div>
    </div>
  );
}
