import { redirect as nextRedirect, permanentRedirect as nextPermanentRedirect } from "next/navigation";
import { getLocale } from "next-intl/server";
import { isLocale, type Locale } from "./config";
import { splitLocale } from "./localePaths";
import { permanentRedirect, redirect } from "./navigation";

// Server-side redirects (Server Components, server actions, route handlers) that keep the visitor
// in the language of the page they're on: redirectTo("/login") from a Russian page goes to
// /ru/login. A path that already carries a language (a ?next= that came from a prefixed page) is
// used as it is. Always `return redirectTo(…)` — TypeScript only narrows after a direct call.
export async function redirectTo(href: string): Promise<never> {
  if (splitLocale(href.split(/[?#]/)[0]).locale) nextRedirect(href);
  const locale = await getLocale();
  return redirect({ href, locale: isLocale(locale) ? locale : "ro" });
}

export async function permanentRedirectTo(href: string): Promise<never> {
  if (splitLocale(href.split(/[?#]/)[0]).locale) nextPermanentRedirect(href);
  const locale = await getLocale();
  return permanentRedirect({ href, locale: isLocale(locale) ? locale : "ro" });
}

// To a path in a given language — after a sign-in, which may switch the site to the account's
// language. A language prefix already on the path gives way to it.
export function redirectInLocale(href: string, locale: Locale): never {
  const [pathname, rest = ""] = splitAt(href);
  return redirect({ href: splitLocale(pathname).path + rest, locale });
}

function splitAt(href: string): [string, string?] {
  const i = href.search(/[?#]/);
  return i < 0 ? [href] : [href.slice(0, i), href.slice(i)];
}
