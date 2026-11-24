import { Fraunces, Inter, Sora } from "next/font/google";
import type { Metadata } from "next";
import { Suspense } from "react";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages, getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";
import { isLocale, locales, type Locale } from "@/i18n/config";
import { Header } from "@/components/layout/Header";
import { MobileTabBar } from "@/components/layout/MobileTabBar";
import { adminWaitingTotal } from "@/lib/admin/adminSections";
import { getAdminSummary } from "@/lib/admin/summaryApi";
import { NavigationTracker } from "@/components/layout/NavigationTracker";
import { AnalyticsProvider } from "@/components/analytics/AnalyticsProvider";
import { ConsentBanner } from "@/components/consent/ConsentBanner";
import { RealtimeProvider } from "@/components/messaging/RealtimeProvider";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { siteUrl } from "@/lib/site";
import { PUBLIC_CONFIG_ELEMENT_ID, serializePublicConfig, serverPublicConfig } from "@/lib/api/publicConfig";
import { getUnreadCount } from "@/lib/messaging/api";
import "../globals.css";

const inter = Inter({
  subsets: ["latin", "latin-ext", "cyrillic"],
  variable: "--font-inter",
  display: "swap",
});

const fraunces = Fraunces({
  subsets: ["latin", "latin-ext"],
  variable: "--font-fraunces",
  display: "swap",
  axes: ["opsz"],
});

// Bold geometric sans for brand headings — the homepage hero and the add/edit listing form (the
// font-hero class, mapped in globals.css) — distinct from font-display (Fraunces, a serif used
// for regular section headings elsewhere). Latin only: Google Fonts ships no Cyrillic for Sora,
// so Russian headings fall back to the system sans-serif.
const sora = Sora({
  subsets: ["latin", "latin-ext"],
  variable: "--font-sora",
  display: "swap",
  weight: ["700", "800"],
});

// Open Graph locales (language_TERRITORY) for each site language.
const OG_LOCALES: Record<Locale, string> = { ro: "ro_MD", ru: "ru_MD", en: "en_US" };

type LayoutParams = { params: Promise<{ locale: string }> };

// What every page looks like outside the site unless it says otherwise: title, description and a
// link preview card (Viber, Telegram, Facebook) with the home photo — in the page's language.
export async function generateMetadata({ params }: LayoutParams): Promise<Metadata> {
  const { locale } = await params;
  if (!isLocale(locale)) return {};
  const t = await getTranslations({ locale, namespace: "Meta" });
  const image = { url: "/images/og-default.jpg", width: 1200, height: 630, alt: t("ogImageAlt") };

  return {
    // Relative links in page metadata (canonical, og:url) resolve against the site's own address.
    metadataBase: new URL(siteUrl()),
    title: t("siteTitle"),
    description: t("siteDescription"),
    openGraph: {
      siteName: "IMOVA",
      locale: OG_LOCALES[locale],
      alternateLocale: locales.filter((l) => l !== locale).map((l) => OG_LOCALES[l]),
      type: "website",
      title: t("siteTitle"),
      description: t("siteDescription"),
      images: [image],
    },
    twitter: { card: "summary_large_image", title: t("siteTitle"), description: t("siteDescription"), images: [image.url] },
  };
}

export default async function RootLayout({
  children,
  params,
}: LayoutParams & {
  children: React.ReactNode;
}) {
  // Only the three languages exist; anything else that lands here (e.g. /favicon.ico) is a 404.
  if (!isLocale((await params).locale)) notFound();

  const [locale, messages, profile] = await Promise.all([getLocale(), getMessages(), getCurrentUserProfile()]);
  const unreadCount = profile ? await getUnreadCount() : 0;
  const adminWaiting = profile?.roles.includes("Admin") ? adminWaitingTotal(await getAdminSummary()) : 0;

  return (
    <html lang={locale} className={`${inter.variable} ${fraunces.variable} ${sora.variable}`}>
      <body>
        {/* Per-environment browser settings (the API's address), read at runtime — lib/api/publicConfig. */}
        <script
          id={PUBLIC_CONFIG_ELEMENT_ID}
          type="application/json"
          dangerouslySetInnerHTML={{ __html: serializePublicConfig(serverPublicConfig()) }}
        />
        <NextIntlClientProvider locale={locale} messages={messages}>
          <RealtimeProvider userId={profile?.id ?? null} initialUnreadCount={unreadCount}>
            <Header />
            {children}
            <MobileTabBar profile={profile} adminWaiting={adminWaiting} />
            <ConsentBanner />
            <AnalyticsProvider userId={profile?.id ?? null} />
            {/* useSearchParams needs a Suspense boundary, or every page would render client-side only. */}
            <Suspense fallback={null}>
              <NavigationTracker />
            </Suspense>
          </RealtimeProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
