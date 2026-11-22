import { getRequestConfig } from "next-intl/server";
import { defaultLocale, isLocale } from "./config";

// The language comes from the address ([locale] segment, set by middleware.ts), never the cookie.
export default getRequestConfig(async ({ requestLocale }) => {
  const requested = await requestLocale;
  const locale = isLocale(requested) ? requested : defaultLocale;

  return {
    locale,
    messages: (await import(`../../messages/${locale}.json`)).default,
  };
});
