import type { Metadata } from "next";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { LegalDocument } from "@/components/legal/LegalDocument";
import { PRIVACY } from "@/content/legal";
import { isLocale } from "@/i18n/config";
import { localeAlternates } from "@/i18n/localePaths";
import { legalLabels } from "@/lib/legal/page";

async function currentLocale() {
  const locale = await getLocale();
  return isLocale(locale) ? locale : "ro";
}

export async function generateMetadata(): Promise<Metadata> {
  const [t, locale] = await Promise.all([getTranslations("Legal"), currentLocale()]);
  return {
    title: t("privacyTitle"),
    description: t("privacyDescription"),
    alternates: localeAlternates(locale, "/privacy"),
  };
}

// The Privacy policy. The text is in src/content/legal (privacy.<locale>.ts).
export default async function PrivacyPage() {
  const [t, locale] = await Promise.all([getTranslations("Legal"), currentLocale()]);

  return (
    <div className="flex min-h-screen flex-col">
      <LegalDocument document={PRIVACY[locale]} labels={legalLabels(t, locale, t("privacyTitle"))} />
      <Footer />
    </div>
  );
}
