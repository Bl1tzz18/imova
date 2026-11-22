"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

type DropdownItem = {
  id: string;
  label: string;
  icon?: ReactNode;
  // A link (opens in a new tab when external) or an action.
  href?: string;
  external?: boolean;
  onSelect?: () => void;
  // For a "pick one" menu (the language): the current choice, ticked and announced as selected.
  checked?: boolean;
  // Small text at the end of the row (e.g. the language code).
  hint?: string;
};

// A button that opens a small menu under it. Opening moves focus to the first item; Escape (back to
// the button) or a click outside closes it. Used for the owner's "⋯" actions, for sharing and for
// the language.
export function DropdownMenu({
  label,
  trigger,
  triggerClassName,
  items,
  disabled = false,
  menuMinWidth = "min-w-52",
}: {
  label: string;
  trigger: ReactNode;
  triggerClassName: string;
  items: DropdownItem[];
  disabled?: boolean;
  menuMinWidth?: string;
}) {
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const button = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    // Focus the current choice in a pick-one menu, else the first item.
    root.current
      ?.querySelector<HTMLElement>('[aria-checked="true"], [role="menuitem"], [role="menuitemradio"]')
      ?.focus();
    function onPointerDown(e: PointerEvent) {
      if (!root.current?.contains(e.target as Node)) setOpen(false);
    }
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        setOpen(false);
        button.current?.focus();
      }
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  const itemClass =
    "flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm text-ink-800 hover:bg-ink-50 focus:bg-ink-50 focus:outline-none";

  return (
    <div ref={root} className="relative shrink-0">
      <button
        ref={button}
        type="button"
        aria-label={label}
        title={label}
        aria-haspopup="menu"
        aria-expanded={open}
        disabled={disabled}
        onClick={() => setOpen((o) => !o)}
        className={triggerClassName}
      >
        {trigger}
      </button>
      {open && (
        <div
          role="menu"
          aria-label={label}
          className={cn("absolute right-0 top-full z-20 mt-1.5 overflow-hidden rounded-xl border border-ink-100 bg-white py-1 shadow-[var(--shadow-card-hover)]", menuMinWidth)}
        >
          {items.map((item) =>
            item.href ? (
              <a
                key={item.id}
                role="menuitem"
                href={item.href}
                target={item.external ? "_blank" : undefined}
                rel={item.external ? "noopener noreferrer" : undefined}
                onClick={() => setOpen(false)}
                className={itemClass}
              >
                {item.icon}
                {item.label}
              </a>
            ) : (
              <button
                key={item.id}
                type="button"
                role={item.checked === undefined ? "menuitem" : "menuitemradio"}
                aria-checked={item.checked}
                onClick={() => {
                  setOpen(false);
                  item.onSelect?.();
                }}
                className={cn(itemClass, item.checked && "bg-accent-100/60 font-semibold text-accent-700 hover:bg-accent-100 focus:bg-accent-100")}
              >
                {item.icon}
                <span className="flex-1">{item.label}</span>
                {item.checked ? (
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
                    <path d="m5 12.5 4.5 4.5L19 7.5" />
                  </svg>
                ) : (
                  item.hint && <span className="text-xs font-semibold text-ink-400">{item.hint}</span>
                )}
              </button>
            ),
          )}
        </div>
      )}
    </div>
  );
}
