"use client";

import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
import { searchLocations, type LocationSuggestion } from "@/lib/api/locationSearch";
import { cn } from "@/lib/utils/cn";

// Typeahead over every raion, localitate and Chișinău sector (diacritics optional). Typing clears a
// previous pick; picking one reports it up.
export function LocationAutocomplete({
  value,
  onChange,
  className,
  placeholder,
  ariaLabel,
}: {
  value: LocationSuggestion | null;
  onChange: (suggestion: LocationSuggestion | null) => void;
  className?: string;
  placeholder: string;
  ariaLabel: string;
}) {
  const t = useTranslations("Search");
  const listId = useId();
  const [text, setText] = useState(value?.name ?? "");
  const [suggestions, setSuggestions] = useState<LocationSuggestion[]>([]);
  const [open, setOpen] = useState(false);
  const [highlighted, setHighlighted] = useState(0);
  const container = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (value && text === value.name) return;
    const controller = new AbortController();
    const timer = setTimeout(() => {
      searchLocations(text, controller.signal)
        .then((found) => {
          setSuggestions(found);
          setHighlighted(0);
        })
        .catch(() => {});
    }, 200);
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [text, value]);

  useEffect(() => {
    function close(e: MouseEvent) {
      if (!container.current?.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  function pick(suggestion: LocationSuggestion) {
    onChange(suggestion);
    setText(suggestion.name);
    setOpen(false);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (!open || suggestions.length === 0) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlighted((i) => Math.min(i + 1, suggestions.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlighted((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      pick(suggestions[highlighted]);
    } else if (e.key === "Escape") {
      setOpen(false);
    }
  }

  const kindLabel = (s: LocationSuggestion) =>
    s.kind === "Raion" ? t("kindRaion") : s.kind === "Sector" ? t("kindSector", { raion: s.raionName }) : s.raionName;

  return (
    <div ref={container} className="relative">
      <input
        type="text"
        role="combobox"
        aria-expanded={open && suggestions.length > 0}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-label={ariaLabel}
        value={text}
        placeholder={placeholder}
        onChange={(e) => {
          setText(e.target.value);
          setOpen(true);
          if (value) onChange(null);
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={handleKeyDown}
        className={className}
      />
      {open && suggestions.length > 0 && (
        <ul id={listId} role="listbox" className="absolute left-0 right-0 top-[calc(100%+4px)] z-30 max-h-72 overflow-y-auto rounded-xl border border-line bg-white py-1 text-left shadow-[var(--shadow-card-hover)]">
          {suggestions.map((s, i) => (
            <li key={`${s.kind}-${s.id}`} role="option" aria-selected={i === highlighted}>
              <button
                type="button"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => pick(s)}
                onMouseEnter={() => setHighlighted(i)}
                className={cn("flex w-full items-baseline justify-between gap-3 px-3.5 py-2 text-left text-sm", i === highlighted && "bg-bubble")}
              >
                <span className="truncate font-medium text-ink-900">{s.name}</span>
                <span className="shrink-0 text-xs text-ink-400">{kindLabel(s)}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
