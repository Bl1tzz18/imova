import type { Locale } from "@/i18n/config";
import type { LegalDocument } from "@/lib/legal/document";
import { privacyEn } from "./privacy.en";
import { privacyRo } from "./privacy.ro";
import { privacyRu } from "./privacy.ru";
import { termsEn } from "./terms.en";
import { termsRo } from "./terms.ro";
import { termsRu } from "./terms.ru";

// The two legal documents in every language. The Romanian text prevails (Terms, "law" section).
export const TERMS: Record<Locale, LegalDocument> = { ro: termsRo, en: termsEn, ru: termsRu };
export const PRIVACY: Record<Locale, LegalDocument> = { ro: privacyRo, en: privacyEn, ru: privacyRu };
