"use client";

import { useEffect, useRef, useState } from "react";
import { TextInput } from "@/components/ui/Field";
import { getStreetSuggestions, type StreetSuggestion } from "@/lib/api/locations";

const MIN_QUERY_LENGTH = 3;
const DEBOUNCE_MS = 350;

// Typeahead aid for the street address field, following the same bespoke click-outside/Escape-to-
// close pattern as SearchableSelect.tsx — but unlike SearchableSelect, this stays a plain editable
// text field: picking a suggestion just fills it, it's not a hard-locked selection, and the field
// submits via its own `name` like any other TextInput (no hidden mirror `<select>` needed).
export function StreetAddressAutocomplete({
  raionId,
  localitateId,
  defaultValue,
  placeholder,
}: {
  raionId?: string;
  localitateId?: string;
  defaultValue?: string | null;
  placeholder?: string;
}) {
  const [value, setValue] = useState(defaultValue ?? "");
  const [suggestions, setSuggestions] = useState<StreetSuggestion[]>([]);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  // Guards a slow response for an earlier keystroke from clobbering a faster response for a later
  // one — only the result matching the most recently *sent* request is applied.
  const requestIdRef = useRef(0);
  // Selecting a suggestion sets `value` to that suggestion's own name, which would otherwise
  // re-trigger this same effect and immediately reopen the dropdown with a redundant self-match —
  // this flag skips exactly that one re-fetch, without suppressing fetches for further typing.
  const skipNextFetchRef = useRef(false);

  useEffect(() => {
    if (skipNextFetchRef.current) {
      skipNextFetchRef.current = false;
      return;
    }

    const query = value.trim();
    if (query.length < MIN_QUERY_LENGTH) {
      setSuggestions([]);
      setOpen(false);
      return;
    }

    const requestId = ++requestIdRef.current;
    const handle = setTimeout(() => {
      getStreetSuggestions(query, raionId, localitateId).then((results) => {
        if (requestId !== requestIdRef.current) return; // a newer keystroke has superseded this
        setSuggestions(results);
        setOpen(results.length > 0);
      });
    }, DEBOUNCE_MS);

    return () => clearTimeout(handle);
  }, [value, raionId, localitateId]);

  useEffect(() => {
    if (!open) return;

    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    function handleEscape(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleEscape);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  function select(suggestion: StreetSuggestion) {
    skipNextFetchRef.current = true;
    setValue(suggestion.name);
    setSuggestions([]);
    setOpen(false);
  }

  return (
    <div ref={containerRef} className="relative">
      <TextInput
        name="streetAddress"
        maxLength={200}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onFocus={() => setOpen(suggestions.length > 0)}
        placeholder={placeholder}
        autoComplete="off"
      />
      {open && (
        <ul
          role="listbox"
          className="absolute left-0 top-[calc(100%+4px)] z-20 max-h-64 w-full overflow-y-auto rounded-xl border border-ink-100 bg-white p-2 shadow-[var(--shadow-card)]"
        >
          {suggestions.map((suggestion) => (
            <li key={suggestion.name}>
              <button
                type="button"
                role="option"
                onClick={() => select(suggestion)}
                className="w-full truncate rounded-lg px-2 py-1.5 text-left text-sm hover:bg-ink-50"
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
