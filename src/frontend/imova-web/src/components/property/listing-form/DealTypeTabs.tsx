"use client";

import { cn } from "@/lib/utils/cn";

type DealTypeTabsProps = {
  name: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
};

export function DealTypeTabs({ name, value, onChange, options }: DealTypeTabsProps) {
  return (
    <div className="inline-flex gap-1 rounded-xl border border-ink-100 bg-ink-50 p-1">
      {options.map((opt) => {
        const active = opt.value === value;
        return (
          <button
            key={opt.value}
            type="button"
            onClick={() => onChange(opt.value)}
            className={cn(
              "rounded-lg px-5 py-2.5 text-sm font-medium transition-colors",
              active ? "bg-accent-500 text-white" : "text-ink-500 hover:text-ink-900"
            )}
          >
            {opt.label}
          </button>
        );
      })}
      <input type="hidden" name={name} value={value} />
    </div>
  );
}
