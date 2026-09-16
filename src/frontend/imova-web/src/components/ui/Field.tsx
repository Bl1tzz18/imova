import type { ComponentPropsWithoutRef } from "react";
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
