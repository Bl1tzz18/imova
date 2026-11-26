// The "Fin" look shared by every dropdown on the site (lists, menus, suggestions, filter panels):
// a white card with soft corners, a hairline edge, layered shadow and a short scale-in; rows with
// rounded highlights inside a small inset. Positioning stays with each component.

export const popoverPanel =
  "animate-pop-in rounded-2xl border border-ink-950/[0.07] bg-white shadow-[var(--shadow-popover)]";

export const popoverItem =
  "flex w-full items-center gap-3 rounded-[10px] px-3 py-2.5 text-left text-sm text-ink-800 transition-colors hover:bg-ink-50 focus-visible:bg-ink-50 focus-visible:outline-none";

export const popoverSearch =
  "h-10 w-full rounded-[10px] border-0 bg-ink-50 px-3 text-sm text-ink-900 outline-none placeholder:text-ink-400 focus:ring-2 focus:ring-brand-500/20";
