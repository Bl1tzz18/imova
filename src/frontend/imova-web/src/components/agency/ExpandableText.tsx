"use client";

import { useId, useState } from "react";
import { cn } from "@/lib/utils/cn";

// Plain text with its line breaks, folded to a few lines when `long` (decided on the server, see
// bioIsLong) with a button to unfold it. The whole text is always in the page, for search engines.
export function ExpandableText({
  text,
  long,
  moreLabel,
  lessLabel,
  className,
}: {
  text: string;
  long: boolean;
  moreLabel: string;
  lessLabel: string;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const id = useId();

  return (
    <div className={className}>
      <p id={id} className={cn("whitespace-pre-line text-sm leading-6 text-ink-700", long && !open && "line-clamp-4")}>
        {text}
      </p>
      {long && (
        <button
          type="button"
          aria-expanded={open}
          aria-controls={id}
          onClick={() => setOpen((o) => !o)}
          className="mt-1 text-sm font-medium text-accent-700 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
        >
          {open ? lessLabel : moreLabel}
        </button>
      )}
    </div>
  );
}
