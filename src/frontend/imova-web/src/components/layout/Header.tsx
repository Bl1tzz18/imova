import Image from "next/image";
import { Link } from "@/i18n/navigation";
import { getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { LanguageSwitcher } from "@/components/layout/LanguageSwitcher";
import { AccountMenu } from "@/components/layout/AccountMenu";
import { HeaderSearch } from "@/components/layout/HeaderSearch";
import { HeaderMessagesLink } from "@/components/messaging/HeaderMessagesLink";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { searchHref } from "@/lib/search/filters";
import { adminWaitingTotal } from "@/lib/admin/adminSections";
import { getAdminSummary } from "@/lib/admin/summaryApi";

// Phone (below md): logo, a "where?" search field and the language — messages, "Adaugă anunț",
// saved listings and the account are in the bottom tab bar (MobileTabBar), like 999.md. From md up:
// logo, the Buy/Rent/Agencies links, then language, messages, "Adaugă anunț" and the account last
// on the right, where most sites keep it.
export async function Header() {
  const [t, tCommon, profile] = await Promise.all([
    getTranslations("Header"),
    getTranslations("Common"),
    getCurrentUserProfile(),
  ]);
  // Admins only: how many things wait in the admin area (the count on "Administrare").
  const adminWaiting = profile?.roles.includes("Admin") ? adminWaitingTotal(await getAdminSummary()) : 0;

  return (
    <header className="sticky top-0 z-30 border-b border-ink-100 bg-ink-50/85 backdrop-blur">
      <div className="mx-auto flex h-20 max-w-6xl items-center justify-between gap-2 px-4 sm:gap-4 sm:px-6">
        <Link href="/" className="flex shrink-0 items-center">
          <Image src="/logo.png" alt="IMOVA" width={460} height={271} priority className="h-9 w-auto sm:h-10 md:h-14" />
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

        <div className="flex min-w-0 flex-1 items-center gap-2 md:hidden">
          <HeaderSearch />
          <LanguageSwitcher />
        </div>

        <div className="hidden min-w-0 items-center gap-3 md:flex">
          <LanguageSwitcher />
          {profile && <HeaderMessagesLink />}
          <LinkButton href="/properties/new" size="sm" className="shrink-0 whitespace-nowrap px-3.5">
            {tCommon("addListing")}
          </LinkButton>
          {profile ? (
            <AccountMenu profile={profile} adminWaiting={adminWaiting} />
          ) : (
            <Link href="/login" className="shrink-0 text-sm font-medium text-ink-600 transition-colors hover:text-ink-950">
              {t("login")}
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
