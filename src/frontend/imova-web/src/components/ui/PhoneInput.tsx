"use client";

import { useEffect, useMemo, useRef, useState, type ChangeEvent } from "react";
import { useLocale, useTranslations } from "next-intl";
import { parsePhoneNumberFromString } from "libphonenumber-js";
import { cn } from "@/lib/utils/cn";
import { inputClass } from "@/components/ui/Field";
import { getCountryList, type Country } from "@/lib/utils/countries";

const DEFAULT_COUNTRY = "MD";

// Splits a stored E.164-ish value like "+37369123456" back into a country + local number, so an
// existing phone can be prefilled for editing (e.g. the account settings Profile tab).
function parseInitialValue(value: string | undefined, fallbackCountry: string) {
  const parsed = value ? parsePhoneNumberFromString(value) : undefined;
  return {
    countryCode: parsed?.country ?? fallbackCountry,
    localNumber: parsed?.nationalNumber ?? "",
  };
}

export function PhoneInput({
  name,
  required,
  defaultCountry = DEFAULT_COUNTRY,
  defaultValue,
  className,
}: {
  name: string;
  required?: boolean;
  defaultCountry?: string;
  defaultValue?: string;
  className?: string;
}) {
  const locale = useLocale();
  const t = useTranslations("Auth");
  const countries = useMemo(() => getCountryList(locale), [locale]);
  const initial = useMemo(() => parseInitialValue(defaultValue, defaultCountry), [defaultValue, defaultCountry]);

  const [countryCode, setCountryCode] = useState(initial.countryCode);
  const country: Country =
    countries.find((c) => c.code === countryCode) ?? countries[0];

  const [localNumber, setLocalNumber] = useState(initial.localNumber);
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

  const filteredCountries = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return countries;
    return countries.filter(
      (c) => c.name.toLowerCase().includes(q) || c.callingCode.includes(q) || c.code.toLowerCase().includes(q),
    );
  }, [countries, query]);

  function handleNumberChange(e: ChangeEvent<HTMLInputElement>) {
    setLocalNumber(e.target.value.replace(/\D/g, ""));
  }

  function selectCountry(c: Country) {
    setCountryCode(c.code);
    setOpen(false);
    setQuery("");
  }

  const combinedValue = localNumber ? `+${country.callingCode}${localNumber}` : "";

  return (
    <div ref={containerRef} className="relative flex gap-2">
      <div className="relative">
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          aria-haspopup="listbox"
          aria-expanded={open}
          className={cn(
            "flex h-11 items-center gap-1.5 rounded-xl border border-ink-200 bg-white px-3 text-sm text-ink-900 outline-none transition-colors hover:border-ink-300 focus-visible:border-brand-500 focus-visible:ring-2 focus-visible:ring-brand-500/20",
          )}
        >
          <span className="text-base leading-none">{country.flag}</span>
          <span className="text-ink-600">+{country.callingCode}</span>
          <span className="text-ink-400">▾</span>
        </button>

        {open && (
          <div className="absolute left-0 top-[calc(100%+4px)] z-20 w-72 rounded-xl border border-ink-100 bg-white p-2 shadow-[var(--shadow-card)]">
            <input
              ref={searchRef}
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={t("phoneSearchPlaceholder")}
              className={cn(inputClass, "mb-2 h-9 text-sm")}
            />
            <ul role="listbox" className="max-h-64 overflow-y-auto">
              {filteredCountries.length === 0 && (
                <li className="px-2 py-2 text-sm text-ink-400">{t("phoneNoResults")}</li>
              )}
              {filteredCountries.map((c) => (
                <li key={c.code}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={c.code === country.code}
                    onClick={() => selectCountry(c)}
                    className={cn(
                      "flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm hover:bg-ink-50",
                      c.code === country.code && "bg-brand-100/60",
                    )}
                  >
                    <span className="text-base leading-none">{c.flag}</span>
                    <span className="flex-1 truncate text-ink-800">{c.name}</span>
                    <span className="text-ink-400">+{c.callingCode}</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <input
        type="tel"
        inputMode="numeric"
        value={localNumber}
        onChange={handleNumberChange}
        placeholder={t("phonePlaceholder")}
        required={required}
        className={cn(inputClass, "flex-1", className)}
      />
      <input type="hidden" name={name} value={combinedValue} />
    </div>
  );
}
