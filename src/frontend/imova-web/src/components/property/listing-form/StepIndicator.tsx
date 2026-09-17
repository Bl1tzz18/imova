"use client";

import { cn } from "@/lib/utils/cn";

export type StepState = "done" | "active" | "upcoming";

export type StepDef = {
  number: number;
  label: string;
  state: StepState;
  clickable: boolean;
};

export function StepIndicator({
  steps,
  onSelect,
}: {
  steps: StepDef[];
  onSelect: (n: number) => void;
}) {
  return (
    <div className="relative mb-10 flex justify-between">
      <div className="absolute left-0 right-0 top-[17px] h-px bg-ink-200" />
      {steps.map((s) => (
        <button
          key={s.number}
          type="button"
          disabled={!s.clickable}
          onClick={() => onSelect(s.number)}
          className={cn(
            "relative z-10 flex flex-1 flex-col items-center gap-2",
            s.clickable ? "cursor-pointer" : "cursor-default"
          )}
        >
          <span
            className={cn(
              "flex h-[34px] w-[34px] items-center justify-center rounded-full border-2 text-sm font-semibold transition-colors",
              s.state === "done" && "border-accent-500 bg-accent-500 text-white",
              s.state === "active" && "border-accent-500 bg-white text-accent-600",
              s.state === "upcoming" && "border-ink-200 bg-white text-ink-400"
            )}
          >
            {s.state === "done" ? (
              <svg
                viewBox="0 0 24 24"
                className="h-3.5 w-3.5"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.4"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M4.5 12.5 9.5 17.5 19.5 6.5" />
              </svg>
            ) : (
              s.number
            )}
          </span>
          <span
            className={cn(
              "hidden text-xs font-medium sm:block",
              s.state === "active" ? "text-ink-900" : "text-ink-400"
            )}
          >
            {s.label}
          </span>
        </button>
      ))}
    </div>
  );
}
