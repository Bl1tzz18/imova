import Image from "next/image";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { LanguageSwitcher } from "@/components/layout/LanguageSwitcher";
import { AccountMenu } from "@/components/layout/AccountMenu";
import { HeaderMessagesLink } from "@/components/messaging/HeaderMessagesLink";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { searchHref } from "@/lib/search/filters";

// On a phone (down to 360 px) everything sits on one row: the logo never shrinks, the language is a
// compact select, "Autentificare" is a person icon, and "Adaugă anunț" stays on one line. The links
// in the middle are desktop-only (phones reach search from the home page).
export async function Header() {
  const [t, tCommon, profile] = await Promise.all([
    getTranslations("Header"),
    getTranslations("Common"),
    getCurrentUserProfile(),
  ]);

  return (
    <header className="sticky top-0 z-30 border-b border-ink-100 bg-ink-50/85 backdrop-blur">
      <div className="mx-auto flex h-20 max-w-6xl items-center justify-between gap-2 px-4 sm:gap-4 sm:px-6">
        <Link href="/" className="flex shrink-0 items-center">
          <Image src="/logo.png" alt="IMOVA" width={460} height={271} priority className="h-10 w-auto sm:h-14" />
        </Link>

        <nav className="hidden items-center gap-6 text-sm font-medium text-ink-600 md:flex">
          <Link href={searchHref({ transactionType: ["Sale"] })} className="transition-colors hover:text-ink-950">
            {t("buy")}
          </Link>
          <Link href={searchHref({ transactionType: ["Rent"] })} className="transition-colors hover:text-ink-950">
            {t("rent")}
          </Link>
          <Link href="/agencies" className="transition-colors hover:text-ink-950">
            {t("agencies")}
          </Link>
        </nav>

        <div className="flex min-w-0 items-center gap-1.5 sm:gap-3">
          <LanguageSwitcher />
          {profile && <HeaderMessagesLink />}
          {profile ? (
            <AccountMenu profile={profile} />
          ) : (
            <Link
              href="/login"
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-medium text-ink-600 transition-colors hover:text-ink-950 sm:h-auto sm:w-auto"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-5 w-5 sm:hidden" aria-hidden>
                <circle cx="12" cy="8" r="4" />
                <path d="M4 21a8 8 0 0 1 16 0" strokeLinecap="round" />
              </svg>
              <span className="sr-only sm:not-sr-only">{t("login")}</span>
            </Link>
          )}
          {/* A shorter label on phones where the full one doesn't fit (Russian); the same words in Romanian. */}
          <LinkButton href="/properties/new" size="sm" className="shrink-0 whitespace-nowrap px-3 sm:px-3.5">
            <span className="sm:hidden">{tCommon("addListingShort")}</span>
            <span className="hidden sm:inline">{tCommon("addListing")}</span>
          </LinkButton>
        </div>
      </div>
    </header>
  );
}
