"use client";

import { useState, type ChangeEvent, type ComponentPropsWithoutRef } from "react";
import { cn } from "@/lib/utils/cn";

const inputClass =
  "h-11 w-full rounded-xl border border-ink-200 bg-white px-3.5 text-sm text-ink-900 outline-none transition-colors placeholder:text-ink-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20";

export function FieldLabel({
  required,
  children,
}: {
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <span className="mb-1.5 block text-sm font-medium text-ink-700">
      {children}
      {required && <span className="ml-0.5 text-accent-600">*</span>}
    </span>
  );
}

export function TextInput({ className, ...props }: ComponentPropsWithoutRef<"input">) {
  return <input {...props} className={cn(inputClass, className)} />;
}

export function TextAreaInput({ className, ...props }: ComponentPropsWithoutRef<"textarea">) {
  return <textarea {...props} className={cn(inputClass, "h-auto resize-y py-2.5", className)} />;
}

export function SelectInput({ className, ...props }: ComponentPropsWithoutRef<"select">) {
  return <select {...props} className={cn(inputClass, className)} />;
}

function groupThousands(digits: string) {
  return digits.replace(/\B(?=(\d{3})+(?!\d))/g, " ");
}

// Displays a live space-grouped number (e.g. "23 450") while typing, but submits the
// raw unformatted value under `name` so the server keeps parsing a plain number string.
export function PriceInput({
  name,
  required,
  defaultValue,
  className,
}: {
  name: string;
  required?: boolean;
  defaultValue?: number | string;
  className?: string;
}) {
  const [raw, setRaw] = useState(defaultValue != null ? String(defaultValue) : "");

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    let value = e.target.value.replace(/[^\d.,]/g, "").replace(/,/g, ".");
    const firstDot = value.indexOf(".");
    if (firstDot !== -1) {
      value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, "");
    }
    setRaw(value);
  };

  const [intPart, decPart] = raw.split(".");
  const display = raw === "" ? "" : `${groupThousands(intPart)}${decPart !== undefined ? `.${decPart}` : raw.endsWith(".") ? "." : ""}`;

  return (
    <>
      <input
        type="text"
        inputMode="decimal"
        value={display}
        onChange={handleChange}
        required={required}
        placeholder="0"
        className={cn(inputClass, className)}
      />
      <input type="hidden" name={name} value={raw} />
    </>
  );
}
