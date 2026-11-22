import { defineRouting } from "next-intl/routing";
import { defaultLocale, localeCookieName, locales } from "./config";

// The language is part of the address: Romanian pages have none (/search), the others a prefix
// (/ru/search, /en/search). Search engines never send cookies, so this is the only way they see
// every language — and a shared link opens in the language it was shared in.
//
// No language detection by next-intl: the browser's Accept-Language never moves anyone (plenty of
// Romanian speakers run phones in English or Russian). The language cookie only steers an
// unprefixed page *visit*, which middleware.ts handles itself (see localeRedirect in localePaths).
export const routing = defineRouting({
  locales,
  defaultLocale,
  localePrefix: "as-needed",
  localeDetection: false,
  localeCookie: { name: localeCookieName, maxAge: 60 * 60 * 24 * 365, sameSite: "lax" },
  // middleware.ts writes the hreflang Link header itself, against SITE_URL (next-intl would use the
  // request's own host — 0.0.0.0:3000 inside docker).
  alternateLinks: false,
});
