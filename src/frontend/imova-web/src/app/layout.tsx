import { Fraunces, Inter, Sora } from "next/font/google";
import type { Metadata } from "next";
import { Suspense } from "react";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";
import { Header } from "@/components/layout/Header";
import { NavigationTracker } from "@/components/layout/NavigationTracker";
import { RealtimeProvider } from "@/components/messaging/RealtimeProvider";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { siteUrl } from "@/lib/site";
import { getUnreadCount } from "@/lib/messaging/api";
import "./globals.css";

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

export const metadata: Metadata = {
  // Relative links in page metadata (canonical, og:url) resolve against the site's own address.
  metadataBase: new URL(siteUrl()),
  title: "IMOVA — Imobiliare în Moldova",
  description: "Imobiliare de la persoane fizice și agenții, în Moldova",
  openGraph: { siteName: "IMOVA", locale: "ro_MD", type: "website" },
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [locale, messages, profile] = await Promise.all([getLocale(), getMessages(), getCurrentUserProfile()]);
  const unreadCount = profile ? await getUnreadCount() : 0;

  return (
    <html lang={locale} className={`${inter.variable} ${fraunces.variable} ${sora.variable}`}>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <RealtimeProvider userId={profile?.id ?? null} initialUnreadCount={unreadCount}>
            <Header />
            {children}
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
