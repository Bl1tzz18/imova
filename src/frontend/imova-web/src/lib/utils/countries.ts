import { getCountries, getCountryCallingCode, type CountryCode } from "libphonenumber-js";

export type Country = {
  code: CountryCode;
  name: string;
  callingCode: string;
  flag: string;
};

// Regional indicator symbols — a two-letter ISO code like "MD" maps to two Unicode code points
// that render as a flag emoji in any font with flag support, no image assets needed.
function flagEmoji(isoCode: string): string {
  return isoCode
    .toUpperCase()
    .replace(/./g, (char) => String.fromCodePoint(127397 + char.charCodeAt(0)));
}

let cache: { locale: string; countries: Country[] } | null = null;

export function getCountryList(locale: string): Country[] {
  if (cache?.locale === locale) {
    return cache.countries;
  }

  const displayNames = new Intl.DisplayNames([locale], { type: "region" });
  const countries = getCountries()
    .map((code) => ({
      code,
      name: displayNames.of(code) ?? code,
      callingCode: getCountryCallingCode(code),
      flag: flagEmoji(code),
    }))
    .sort((a, b) => a.name.localeCompare(b.name, locale));

  cache = { locale, countries };
  return countries;
}

export function findCountry(locale: string, code: CountryCode): Country | undefined {
  return getCountryList(locale).find((c) => c.code === code);
}
