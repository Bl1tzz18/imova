"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils/cn";
import { normalizeForSearch } from "@/lib/utils/search";
import { inputClass } from "@/components/ui/Field";

export type SearchableSelectOption = {
  id: string;
  label: string;
};

// Searchable/typeahead dropdown for a large option list, following the same bespoke
// click-outside/Escape-to-close/autofocus-search pattern as PhoneInput.tsx's country picker (no
// combobox/autocomplete library exists in this project yet, and one isn't warranted just for
// this). Diacritic-insensitive filtering via normalizeForSearch.
//
// A visually-hidden real <select> (not display:none, not type=hidden — both would be barred from
// constraint validation) mirrors the selection for native FormData submission and `required`
// support: PropertyForm's step-by-step validation walks the current step's DOM for `:invalid`
// elements, which only works against a real form control, not a plain hidden input (see
// DealTypeTabs.tsx for the simpler case, which never needs `required` since it always has a
// valid default).
export function SearchableSelect({
  name,
  value,
  onChange,
  options,
  placeholder,
  searchPlaceholder,
  noResultsText,
  disabled,
  required,
}: {
  name: string;
  value: string;
  onChange: (id: string) => void;
  options: SearchableSelectOption[];
  placeholder: string;
  searchPlaceholder: string;
  noResultsText: string;
  disabled?: boolean;
  required?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");

  const containerRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

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
    searchRef.current?.focus();

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  const filteredOptions = useMemo(() => {
    const q = normalizeForSearch(query.trim());
    if (!q) return options;
    return options.filter((o) => normalizeForSearch(o.label).includes(q));
  }, [options, query]);

  const selected = options.find((o) => o.id === value);

  function select(option: SearchableSelectOption) {
    onChange(option.id);
    setOpen(false);
    setQuery("");
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="listbox"
        aria-expanded={open}
        className={cn(
          inputClass,
          "flex items-center justify-between text-left disabled:cursor-not-allowed disabled:opacity-60",
        )}
      >
        <span className={cn("truncate", !selected && "text-ink-400")}>
          {selected ? selected.label : placeholder}
        </span>
        <span className="ml-2 shrink-0 text-ink-400">▾</span>
      </button>

      {open && (
        <div className="absolute left-0 top-[calc(100%+4px)] z-20 w-full min-w-[16rem] rounded-xl border border-ink-100 bg-white p-2 shadow-[var(--shadow-card)]">
          <input
            ref={searchRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={searchPlaceholder}
            className={cn(inputClass, "mb-2 h-9 text-sm")}
          />
          <ul role="listbox" className="max-h-64 overflow-y-auto">
            {filteredOptions.length === 0 && <li className="px-2 py-2 text-sm text-ink-400">{noResultsText}</li>}
            {filteredOptions.map((option) => (
              <li key={option.id}>
                <button
                  type="button"
                  role="option"
                  aria-selected={option.id === value}
                  onClick={() => select(option)}
                  className={cn(
                    "w-full truncate rounded-lg px-2 py-1.5 text-left text-sm hover:bg-ink-50",
                    option.id === value && "bg-brand-100/60",
                  )}
                >
                  {option.label}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      <select
        name={name}
        required={required}
        value={value}
        onChange={() => {}}
        className="sr-only"
        aria-hidden="true"
        tabIndex={-1}
      >
        <option value="" />
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}
