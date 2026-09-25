"use client";

import { useEffect, useRef, useState } from "react";
import { inputClass } from "@/components/ui/Field";
import { cn } from "@/lib/utils/cn";

const DEBOUNCE_MS = 600;

// A number filter that only commits once typing pauses (or on blur / Enter) — not a search per
// keystroke. Follows the URL when it changes from elsewhere (e.g. "Resetează").
export function DebouncedNumberInput({
  value,
  onCommit,
  placeholder,
  ariaLabel,
  min,
  allowNegative = false,
}: {
  value: string;
  onCommit: (value: string) => void;
  placeholder: string;
  ariaLabel: string;
  min?: number;
  allowNegative?: boolean;
}) {
  const [draft, setDraft] = useState(value);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => setDraft(value), [value]);
  useEffect(() => () => clearTimeout(timer.current ?? undefined), []);

  function commit(next: string) {
    clearTimeout(timer.current ?? undefined);
    if (next !== value) onCommit(next);
  }

  return (
    <input
      type="number"
      inputMode={allowNegative ? "text" : "numeric"}
      min={min}
      value={draft}
      placeholder={placeholder}
      aria-label={ariaLabel}
      onChange={(e) => {
        const next = e.target.value;
        setDraft(next);
        clearTimeout(timer.current ?? undefined);
        timer.current = setTimeout(() => commit(next), DEBOUNCE_MS);
      }}
      onBlur={() => commit(draft)}
      onKeyDown={(e) => e.key === "Enter" && commit(draft)}
      className={cn(inputClass, "h-10 min-w-0 px-3 text-sm")}
    />
  );
}
