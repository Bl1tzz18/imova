import { Fraunces, Inter, Sora } from "next/font/google";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";
import { Header } from "@/components/layout/Header";
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

export const metadata = {
  title: "IMOVA — Imobiliare în Moldova",
  description: "Imobiliare de la persoane fizice și agenții, în Moldova",
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale} className={`${inter.variable} ${fraunces.variable} ${sora.variable}`}>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <Header />
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
