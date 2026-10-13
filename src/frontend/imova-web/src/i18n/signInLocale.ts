import { defaultLocale, isLocale, type Locale } from "./config";

// Set by the language switcher while signed out: the visitor picked this device's language on
// purpose. Only that counts as a choice — a NEXT_LOCALE cookie left by an earlier sign-in doesn't.
export const localeChosenCookieName = "imova_locale_chosen";

// Which language the site uses after a sign-in, and whether the account must follow it:
// - the visitor chose a language on this device before signing in → keep it, and save it on the
//   account when it differs;
// - otherwise → the account's own language (Romanian when the account's value is unusable).
export function localeAfterSignIn(
  accountLanguage: string | undefined,
  chosenLocale: string | undefined,
): { locale: Locale; saveToAccount: boolean } {
  if (isLocale(chosenLocale)) {
    return { locale: chosenLocale, saveToAccount: chosenLocale !== accountLanguage };
  }

  return { locale: isLocale(accountLanguage) ? accountLanguage : defaultLocale, saveToAccount: false };
}
