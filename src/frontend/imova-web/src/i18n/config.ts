export const locales = ["ro", "en", "ru"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "ro";
// Each language in its own language, never translated (someone who can't read the current page
// still finds theirs), in the order the language menu lists them: the two everyday languages first.
export const LOCALE_NAMES: Record<Locale, string> = { ro: "Română", ru: "Русский", en: "English" };
export const LOCALE_MENU_ORDER: readonly Locale[] = ["ro", "ru", "en"];
export const localeCookieName = "NEXT_LOCALE";

export function isLocale(value: string | undefined): value is Locale {
  return !!value && (locales as readonly string[]).includes(value);
}
