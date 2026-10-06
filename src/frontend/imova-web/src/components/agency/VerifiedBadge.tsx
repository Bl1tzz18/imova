import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";

// The check mark of an agency IMOVA has verified. `label`: show "Verificată" next to it (the agency's
// own page); otherwise only the icon, still named for screen readers and with the tooltip.
export function VerifiedBadge({ label = false, className }: { label?: boolean; className?: string }) {
  const t = useTranslations("Agencies");
  return (
    <span
      title={t("verifiedTooltip")}
      className={cn(
        "inline-flex shrink-0 items-center gap-1 text-brand-700",
        label && "rounded-full bg-brand-50 px-2.5 py-1 text-xs font-medium",
        className,
      )}
    >
      <svg viewBox="0 0 24 24" className={label ? "h-3.5 w-3.5" : "h-4 w-4"} aria-hidden>
        <path
          fill="currentColor"
          d="M12 2.5 14.4 4l2.8-.2 1.2 2.6 2.5 1.3-.2 2.8L22 12.9l-1.5 2.4.2 2.8-2.6 1.2-1.3 2.5-2.8-.2L12 23l-2.4-1.5-2.8.2-1.2-2.6-2.5-1.3.2-2.8L2 12.9l1.5-2.4L3.3 7.7l2.6-1.2L7.2 4l2.8.2Z"
        />
        <path d="m8 12.5 2.7 2.7L16.2 9.7" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      {label ? t("verified") : <span className="sr-only">{t("verifiedTooltip")}</span>}
    </span>
  );
}
