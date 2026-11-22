import Image from "next/image";
import { Link } from "@/i18n/navigation";
import { useTranslations } from "next-intl";

export function Footer() {
  const t = useTranslations("Footer");

  return (
    <footer className="border-t border-ink-100 bg-white">
      <div className="mx-auto flex max-w-6xl flex-col items-center gap-3 px-4 py-10 text-center sm:px-6">
        <Image src="/logo.png" alt="IMOVA" width={460} height={271} className="h-11 w-auto" />
        <p className="max-w-sm text-sm text-ink-500">{t("tagline")}</p>
        <div className="flex flex-wrap justify-center gap-x-6 gap-y-2 pt-2 text-sm text-ink-600">
          <Link href="/search" className="hover:text-ink-950">
            {t("searchListings")}
          </Link>
          <Link href="/agencies" className="hover:text-ink-950">
            {t("agencies")}
          </Link>
          <Link href="/properties/new" className="hover:text-ink-950">
            {t("publishListing")}
          </Link>
        </div>
        <div className="flex flex-wrap justify-center gap-x-5 gap-y-2 text-xs text-ink-500">
          <Link href="/terms" className="hover:text-ink-950">
            {t("terms")}
          </Link>
          <Link href="/privacy" className="hover:text-ink-950">
            {t("privacy")}
          </Link>
          <Link href="/privacy#cookies" className="hover:text-ink-950">
            {t("cookies")}
          </Link>
        </div>
        <p className="pt-2 text-xs text-ink-400">
          © {new Date().getFullYear()} IMOVA. {t("inDevelopment")}
        </p>
      </div>
    </footer>
  );
}
