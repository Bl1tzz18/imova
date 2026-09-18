import Image from "next/image";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { LanguageSwitcher } from "@/components/layout/LanguageSwitcher";
import { LogoutButton } from "@/components/auth/LogoutButton";
import { getSessionToken } from "@/lib/auth/session";

export async function Header() {
  const [t, tCommon, token] = await Promise.all([
    getTranslations("Header"),
    getTranslations("Common"),
    getSessionToken(),
  ]);

  return (
    <header className="sticky top-0 z-30 border-b border-ink-100 bg-ink-50/85 backdrop-blur">
      <div className="mx-auto flex h-20 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link href="/" className="flex items-center">
          <Image src="/logo.png" alt="IMOVA" width={460} height={271} priority className="h-14 w-auto" />
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
          {token ? (
            <>
              <Link
                href="/account"
                aria-label={t("account")}
                title={t("account")}
                className="flex h-9 w-9 items-center justify-center rounded-full border border-ink-100 bg-white text-ink-600 transition-colors hover:border-ink-200 hover:text-ink-950"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4.5 w-4.5">
                  <circle cx="12" cy="8.5" r="3.4" />
                  <path d="M5 20c1.2-4 4-6 7-6s5.8 2 7 6" strokeLinecap="round" />
                </svg>
              </Link>
              <LogoutButton>{t("logout")}</LogoutButton>
            </>
          ) : (
            <Link href="/login" className="text-sm font-medium text-ink-600 transition-colors hover:text-ink-950">
              {t("login")}
            </Link>
          )}
          <LinkButton href="/properties/new" size="sm">
            {tCommon("addListing")}
          </LinkButton>
        </div>
      </div>
    </header>
  );
}
