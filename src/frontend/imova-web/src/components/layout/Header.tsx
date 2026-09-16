import Link from "next/link";
import { useTranslations } from "next-intl";
import { LinkButton } from "@/components/ui/Button";
import { LanguageSwitcher } from "@/components/layout/LanguageSwitcher";

export function Header() {
  const t = useTranslations("Header");
  const tCommon = useTranslations("Common");

  return (
    <header className="sticky top-0 z-30 border-b border-ink-100 bg-ink-50/85 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link href="/" className="flex items-center gap-2 font-display text-xl font-semibold tracking-tight text-ink-950">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-accent-500 text-sm font-bold text-white">
            IM
          </span>
          IMOVA
        </Link>

        <nav className="hidden items-center gap-6 text-sm font-medium text-ink-600 md:flex">
          <Link href="/search?listingType=Sale" className="transition-colors hover:text-ink-950">
            {t("buy")}
          </Link>
          <Link href="/search?listingType=Rent" className="transition-colors hover:text-ink-950">
            {t("rent")}
          </Link>
        </nav>

        <div className="flex items-center gap-3">
          <LanguageSwitcher />
          <LinkButton href="/properties/new" size="sm">
            {tCommon("addListing")}
          </LinkButton>
        </div>
      </div>
    </header>
  );
}
