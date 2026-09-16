import Link from "next/link";
import { useTranslations } from "next-intl";

export function Footer() {
  const t = useTranslations("Footer");

  return (
    <footer className="border-t border-ink-100 bg-white">
      <div className="mx-auto flex max-w-6xl flex-col items-center gap-3 px-4 py-10 text-center sm:px-6">
        <span className="flex items-center gap-2 font-display text-lg font-semibold text-ink-950">
          <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-accent-500 text-xs font-bold text-white">
            IM
          </span>
          IMOVA
        </span>
        <p className="max-w-sm text-sm text-ink-500">{t("tagline")}</p>
        <div className="flex gap-6 pt-2 text-sm text-ink-600">
          <Link href="/cauta" className="hover:text-ink-950">
            {t("searchListings")}
          </Link>
          <Link href="/properties/new" className="hover:text-ink-950">
            {t("publishListing")}
          </Link>
        </div>
        <p className="pt-4 text-xs text-ink-400">
          © {new Date().getFullYear()} IMOVA. {t("inDevelopment")}
        </p>
      </div>
    </footer>
  );
}
