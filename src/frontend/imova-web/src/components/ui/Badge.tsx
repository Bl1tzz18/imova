import { cn } from "@/lib/utils/cn";

const tones = {
  brand: "bg-brand-50 text-brand-800",
  accent: "bg-accent-100 text-accent-700",
  neutral: "bg-ink-100 text-ink-700",
} as const;

export function Badge({
  tone = "neutral",
  className,
  children,
}: {
  tone?: keyof typeof tones;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium",
        tones[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}
