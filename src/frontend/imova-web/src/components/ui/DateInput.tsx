"use client";

import { useEffect, useRef, useState, type ChangeEvent } from "react";
import { useLocale } from "next-intl";
import { inputClass } from "@/components/ui/Field";
import { cn } from "@/lib/utils/cn";
import { digitsAfterEdit, formatDayFirst } from "@/lib/utils/dayFirstDate";

// Day-first everywhere this site is offered (ro-MD, ru, and en as British-style dd/mm/yyyy —
// the audience is Moldovan, US month-first ordering would read as a different date).
const SEPARATOR_BY_LOCALE: Record<string, string> = { ro: ".", ru: ".", en: "/" };

function separatorFor(locale: string): string {
  return SEPARATOR_BY_LOCALE[locale.split("-")[0]] ?? ".";
}

// Full dd/mm/yyyy digits -> "yyyy-mm-dd", or null when it isn't a real calendar date.
function toIso(digits: string): string | null {
  if (digits.length !== 8) return null;
  const day = Number(digits.slice(0, 2));
  const month = Number(digits.slice(2, 4));
  const year = Number(digits.slice(4, 8));
  if (year < 1900 || year > 2100) return null;
  const date = new Date(Date.UTC(year, month - 1, day));
  if (date.getUTCFullYear() !== year || date.getUTCMonth() !== month - 1 || date.getUTCDate() !== day) return null;
  return `${digits.slice(4, 8)}-${digits.slice(2, 4)}-${digits.slice(0, 2)}`;
}

function isoToDigits(iso: string | undefined): string {
  const match = iso?.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return match ? `${match[3]}${match[2]}${match[1]}` : "";
}

// A date field that displays in the site's selected language rather than the browser's — a
// native <input type="date"> always renders in the browser locale (e.g. mm/dd/yyyy in an
// English-US browser on the Romanian site). What gets submitted under `name` is still ISO
// yyyy-mm-dd; an incomplete/impossible date marks the visible field :invalid so the form's
// normal step validation reports it. The calendar button reuses the browser's native picker.
export function DateInput({
  name,
  defaultValue,
  placeholder,
  invalidMessage,
  pickerLabel,
}: {
  name: string;
  // ISO date (or datetime) — only the yyyy-mm-dd part is used.
  defaultValue?: string | null;
  placeholder: string;
  invalidMessage: string;
  pickerLabel: string;
}) {
  const separator = separatorFor(useLocale());
  const [digits, setDigits] = useState(() => isoToDigits(defaultValue ?? undefined));
  const textRef = useRef<HTMLInputElement>(null);
  const pickerRef = useRef<HTMLInputElement>(null);

  const iso = toIso(digits);
  const isValid = digits === "" || iso !== null;

  useEffect(() => {
    textRef.current?.setCustomValidity(isValid ? "" : invalidMessage);
  }, [isValid, invalidMessage]);

  function handleTextChange(e: ChangeEvent<HTMLInputElement>) {
    setDigits(digitsAfterEdit(digits, formatDayFirst(digits, separator), e.target.value));
  }

  function openPicker() {
    const picker = pickerRef.current;
    if (!picker) return;
    try {
      picker.showPicker();
    } catch {
      // Older browsers without showPicker() — typing still works.
      textRef.current?.focus();
    }
  }

  return (
    <div className="relative">
      <input
        ref={textRef}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        value={formatDayFirst(digits, separator)}
        onChange={handleTextChange}
        placeholder={placeholder}
        className={cn(inputClass, "pr-11")}
      />
      <input type="hidden" name={name} value={iso ?? ""} />
      <input
        ref={pickerRef}
        type="date"
        tabIndex={-1}
        aria-hidden="true"
        value={iso ?? ""}
        onChange={(e) => setDigits(isoToDigits(e.target.value))}
        className="pointer-events-none absolute bottom-0 right-0 h-0 w-0 opacity-0"
      />
      <button
        type="button"
        onClick={openPicker}
        aria-label={pickerLabel}
        className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-ink-400 transition-colors hover:text-ink-700"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-[18px] w-[18px]">
          <rect x="3.5" y="5" width="17" height="15.5" rx="2" />
          <path d="M3.5 9.5h17M8 3v4M16 3v4" strokeLinecap="round" />
        </svg>
      </button>
    </div>
  );
}
