"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

export type DropdownItem = {
  id: string;
  label: string;
  icon?: ReactNode;
  // A link (opens in a new tab when external) or an action.
  href?: string;
  external?: boolean;
  onSelect?: () => void;
};

// A button that opens a small menu under it. Opening moves focus to the first item; Escape (back to
// the button) or a click outside closes it. Used for the owner's "⋯" actions and for sharing.
export function DropdownMenu({
  label,
  trigger,
  triggerClassName,
  items,
  disabled = false,
}: {
  label: string;
  trigger: ReactNode;
  triggerClassName: string;
  items: DropdownItem[];
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const button = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    root.current?.querySelector<HTMLElement>('[role="menuitem"]')?.focus();
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
          className="absolute right-0 top-full z-20 mt-1.5 min-w-52 overflow-hidden rounded-xl border border-ink-100 bg-white py-1 shadow-[var(--shadow-card-hover)]"
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
                role="menuitem"
                onClick={() => {
                  setOpen(false);
                  item.onSelect?.();
                }}
                className={cn(itemClass)}
              >
                {item.icon}
                {item.label}
              </button>
            ),
          )}
        </div>
      )}
    </div>
  );
}
