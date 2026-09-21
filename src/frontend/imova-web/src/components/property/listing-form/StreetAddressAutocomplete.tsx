"use client";

import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
import { TextInput } from "@/components/ui/Field";
import { cn } from "@/lib/utils/cn";
import { getStreetSuggestions, type StreetSuggestion } from "@/lib/api/locations";

// 3, not 2: tested against the real Photon API during development — at 2 characters, once a
// locality bias is folded into the query (see getStreetSuggestions/GetStreetSuggestionsHandler,
// and note a bare Raion selection now always supplies one), Photon's fuzzy matching degrades and
// starts returning results that don't even contain the typed prefix (e.g. "Is" + a "Soroca" bias
// returned "Șorogari-Cuza Vodă", nowhere near "Is"). At 3+ characters this doesn't happen.
const MIN_QUERY_LENGTH = 3;
// Photon's public instance is purpose-built for typeahead traffic (unlike Nominatim, which this
// app deliberately avoids for autocomplete — see PhotonStreetSuggestionService), so a short
// debounce is fine here. Paired with the AbortController below (so a superseded request is
// actually cancelled, not just ignored) and GetStreetSuggestionsHandler's own short-lived
// server-side cache, this doesn't translate into materially more upstream Photon load.
const DEBOUNCE_MS = 200;

// Typeahead aid for the street address field, following the same bespoke click-outside/Escape-to-
// close pattern as SearchableSelect.tsx — but unlike SearchableSelect, this stays a plain editable
// text field: picking a suggestion just fills it, it's not a hard-locked selection, and the field
// submits via its own `name` like any other TextInput (no hidden mirror `<select>` needed).
export function StreetAddressAutocomplete({
  raionId,
  localitateId,
  defaultValue,
  placeholder,
  required,
}: {
  raionId?: string;
  localitateId?: string;
  defaultValue?: string | null;
  placeholder?: string;
  required?: boolean;
}) {
  const t = useTranslations("PropertyForm");
  const [value, setValue] = useState(defaultValue ?? "");
  const [suggestions, setSuggestions] = useState<StreetSuggestion[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  // Defaults to the first suggestion (index 0), matching SearchableSelect.tsx's choice for every
  // other typeahead dropdown in this form — lets Enter select the top result right after typing,
  // no extra ArrowDown needed.
  const [highlightedIndex, setHighlightedIndex] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  const optionRefs = useRef<(HTMLButtonElement | null)[]>([]);
  // Guards a slow response for an earlier keystroke from clobbering a faster response for a later
  // one — only the result matching the most recently *sent* request is applied.
  const requestIdRef = useRef(0);
  // Selecting a suggestion sets `value` to that suggestion's own name, which would otherwise
  // re-trigger this same effect and immediately reopen the dropdown with a redundant self-match —
  // this flag skips exactly that one re-fetch, without suppressing fetches for further typing.
  const skipNextFetchRef = useRef(false);
  // Tracks the in-flight request so a newer keystroke can actually cancel it (not just ignore its
  // result) — with a 200ms debounce, overlapping requests are common at normal typing speed if
  // Photon's response takes longer than the gap between keystrokes.
  const abortControllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (skipNextFetchRef.current) {
      skipNextFetchRef.current = false;
      return;
    }

    const query = value.trim();
    if (query.length < MIN_QUERY_LENGTH) {
      abortControllerRef.current?.abort();
      setLoading(false);
      setSuggestions([]);
      setOpen(false);
      return;
    }

    const requestId = ++requestIdRef.current;
    const handle = setTimeout(() => {
      // Cancel whatever's still in flight from an earlier keystroke — it's about to be
      // superseded, so the network request itself is cancelled rather than just having its
      // result discarded once it resolves.
      abortControllerRef.current?.abort();
      const controller = new AbortController();
      abortControllerRef.current = controller;

      setLoading(true);
      setOpen(true);
      getStreetSuggestions(query, raionId, localitateId, controller.signal).then((results) => {
        if (requestId !== requestIdRef.current) return; // a newer keystroke has superseded this
        setLoading(false);
        setSuggestions(results);
        setOpen(results.length > 0);
      });
    }, DEBOUNCE_MS);

    return () => clearTimeout(handle);
  }, [value, raionId, localitateId]);

  // Cancel any still-in-flight request on unmount, so navigating away mid-search doesn't leave a
  // dangling Photon request running for nothing.
  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    if (!open) return;

    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    function handleEscape(e: globalThis.KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleEscape);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  // Resets to the first suggestion whenever the list changes (a new fetch resolved) or the
  // dropdown opens/closes — the highlight should never point at a stale or now-hidden suggestion.
  useEffect(() => {
    setHighlightedIndex(0);
  }, [suggestions, open]);

  useEffect(() => {
    if (!open) return;
    optionRefs.current[highlightedIndex]?.scrollIntoView({ block: "nearest" });
  }, [highlightedIndex, open]);

  function select(suggestion: StreetSuggestion) {
    skipNextFetchRef.current = true;
    setValue(suggestion.name);
    setSuggestions([]);
    setOpen(false);
  }

  function moveHighlight(delta: number) {
    if (suggestions.length === 0) return;
    setHighlightedIndex((i) => (i + delta + suggestions.length) % suggestions.length);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (!open || suggestions.length === 0) return;

    if (e.key === "ArrowDown") {
      e.preventDefault();
      moveHighlight(1);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      moveHighlight(-1);
    } else if (e.key === "Enter") {
      const suggestion = suggestions[highlightedIndex];
      if (suggestion) {
        e.preventDefault();
        select(suggestion);
      }
    }
  }

  return (
    <div ref={containerRef} className="relative">
      <TextInput
        name="streetAddress"
        maxLength={200}
        required={required}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onFocus={() => setOpen(loading || suggestions.length > 0)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        autoComplete="off"
      />
      {open && (loading || suggestions.length > 0) && (
        <ul
          role="listbox"
          className="absolute left-0 top-[calc(100%+4px)] z-20 max-h-64 w-full overflow-y-auto rounded-xl border border-ink-100 bg-white p-2 shadow-[var(--shadow-card)]"
        >
          {loading && (
            <li className="flex items-center gap-2 px-2 py-1.5 text-xs text-ink-400" aria-live="polite">
              <span
                className="h-3 w-3 shrink-0 animate-spin rounded-full border-2 border-ink-300 border-t-transparent"
                aria-hidden="true"
              />
              {t("streetSearching")}
            </li>
          )}
          {suggestions.map((suggestion, index) => (
            <li key={suggestion.name}>
              <button
                ref={(el) => {
                  optionRefs.current[index] = el;
                }}
                type="button"
                role="option"
                aria-selected={index === highlightedIndex}
                onClick={() => select(suggestion)}
                onMouseEnter={() => setHighlightedIndex(index)}
                className={cn(
                  "w-full truncate rounded-lg px-2 py-1.5 text-left text-sm hover:bg-ink-50",
                  index === highlightedIndex && "bg-ink-50",
                )}
              >
                {suggestion.name}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
