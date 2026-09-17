import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

export function Checkbox({
  className,
  children,
  ...props
}: ComponentPropsWithoutRef<"input"> & { children: ReactNode }) {
  return (
    <label className={cn("flex cursor-pointer items-start gap-2 text-sm text-ink-500", className)}>
      <input
        type="checkbox"
        className="mt-0.5 h-3.5 w-3.5 shrink-0 rounded border-ink-300 text-brand-600 focus:ring-2 focus:ring-brand-500/30 focus:ring-offset-0"
        {...props}
      />
      <span>{children}</span>
    </label>
  );
}
