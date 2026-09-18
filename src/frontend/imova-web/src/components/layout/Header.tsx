import Image from "next/image";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { LanguageSwitcher } from "@/components/layout/LanguageSwitcher";
import { AccountMenu } from "@/components/layout/AccountMenu";
import { getCurrentUserProfile } from "@/lib/auth/profile";

export async function Header() {
  const [t, tCommon, profile] = await Promise.all([
    getTranslations("Header"),
    getTranslations("Common"),
    getCurrentUserProfile(),
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
          {profile ? (
            <AccountMenu profile={profile} />
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
