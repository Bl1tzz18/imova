import { defaultLocale, type Locale } from "@/i18n/config";
import { LEGAL_VERSION } from "@/lib/legal/version";
import { formatDate } from "@/lib/utils/format";
import type { LegalDocumentLabels } from "@/components/legal/LegalDocument";

type Translate = (key: string, values?: Record<string, string>) => string;

// The page chrome shared by /terms and /privacy, from the "Legal" namespace.
export function legalLabels(t: Translate, locale: Locale, title: string): LegalDocumentLabels {
  return {
    title,
    updated: t("updated", { date: formatDate(LEGAL_VERSION, locale) }),
    contents: t("contents"),
    draftNotice: t("draftNotice"),
    translationNotice: locale === defaultLocale ? undefined : t("translationNotice"),
  };
}
