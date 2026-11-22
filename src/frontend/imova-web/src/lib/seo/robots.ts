import type { MetadataRoute } from "next";
import { locales } from "@/i18n/config";
import { localizedPath } from "@/i18n/localePaths";

// What search engines may crawl (robots.txt): everything public; never the pages behind a sign-in,
// the sign-in pages themselves or one-time links (email confirmation, password reset, invitations)
// — in every language. Pure (Vitest-covered).
export const PRIVATE_PATHS = [
  "/account",
  "/admin",
  "/complete-profile",
  "/confirm-email",
  "/dashboard",
  "/delete-account",
  "/favorites",
  "/forgot-password",
  "/invitations",
  "/login",
  "/message-attachments",
  "/messages",
  "/my-listings",
  "/properties/new",
  "/register",
  "/reset-password",
  "/saved-listings",
  "/saved-searches",
] as const;

export function robotsRules(siteUrl: string): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: locales.flatMap((locale) => PRIVATE_PATHS.map((path) => localizedPath(locale, path))),
    },
    sitemap: `${siteUrl}/sitemap.xml`,
  };
}
