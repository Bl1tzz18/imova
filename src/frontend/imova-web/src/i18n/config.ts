export const locales = ["ro", "en", "ru"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "ro";
export const localeCookieName = "NEXT_LOCALE";

export const localeLabels: Record<Locale, string> = {
  ro: "Română",
  en: "English",
  ru: "Русский",
};

export function isLocale(value: string | undefined): value is Locale {
  return !!value && (locales as readonly string[]).includes(value);
}
