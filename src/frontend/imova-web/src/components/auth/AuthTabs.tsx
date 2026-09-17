import Link from "next/link";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";

const tabClass = "flex-1 rounded-lg px-4 py-2.5 text-center text-sm font-medium transition-colors";

export function AuthTabs({ active, next }: { active: "login" | "register"; next?: string }) {
  const t = useTranslations("Auth");
  const suffix = next ? `?next=${encodeURIComponent(next)}` : "";

  return (
    <div className="mb-6 flex gap-1 rounded-xl border border-ink-100 bg-ink-50 p-1">
      <Link
        href={`/login${suffix}`}
        className={cn(tabClass, active === "login" ? "bg-accent-500 text-white" : "text-ink-500 hover:text-ink-900")}
      >
        {t("loginTab")}
      </Link>
      <Link
        href={`/register${suffix}`}
        className={cn(tabClass, active === "register" ? "bg-accent-500 text-white" : "text-ink-500 hover:text-ink-900")}
      >
        {t("registerTab")}
      </Link>
    </div>
  );
}
