import type { Metadata } from "next";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { LegalDocument } from "@/components/legal/LegalDocument";
import { TERMS } from "@/content/legal";
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
    title: t("termsTitle"),
    description: t("termsDescription"),
    alternates: localeAlternates(locale, "/terms"),
  };
}

// The Terms of use. The text is in src/content/legal (terms.<locale>.ts).
export default async function TermsPage() {
  const [t, locale] = await Promise.all([getTranslations("Legal"), currentLocale()]);

  return (
    <div className="flex min-h-screen flex-col">
      <LegalDocument document={TERMS[locale]} labels={legalLabels(t, locale, t("termsTitle"))} />
      <Footer />
    </div>
  );
}
