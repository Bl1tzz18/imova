"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { useTranslations } from "next-intl";
import type { Property } from "@/types/property";

// Leaflet touches `window` on import, so it can only ever run in the browser — dynamic import
// with ssr:false is only valid from a Client Component, hence this wrapper around the server-fetched
// homepage data.
const PropertyMapPreview = dynamic(
  () => import("@/components/property/PropertyMapPreview").then((m) => m.PropertyMapPreview),
  { ssr: false, loading: () => <div className="h-full w-full animate-pulse bg-ink-100" /> },
);

export function PropertyMapPromo({ properties }: { properties: Property[] }) {
  const t = useTranslations("MapPromo");

  return (
    <Link
      href="/map"
      className="group flex flex-col overflow-hidden rounded-3xl border border-ink-100 bg-white shadow-[var(--shadow-card)] transition-all hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)] sm:flex-row sm:items-stretch"
    >
      <div className="relative h-48 w-full shrink-0 overflow-hidden sm:h-auto sm:w-2/5">
        <PropertyMapPreview properties={properties} />
        <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-white/10 via-transparent to-transparent sm:bg-gradient-to-r" />
      </div>

      <div className="flex flex-1 flex-col justify-center gap-3 p-6 sm:p-8">
        <span className="flex h-10 w-10 items-center justify-center rounded-full bg-accent-100 text-accent-700">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-5 w-5">
            <path d="M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z" />
            <circle cx="12" cy="10" r="2.5" />
          </svg>
        </span>
        <h2 className="font-display text-xl font-medium text-ink-950 sm:text-2xl">{t("title")}</h2>
        <p className="max-w-md text-sm text-ink-500">{t("subtitle")}</p>
        <span className="mt-2 inline-flex w-fit items-center gap-2 rounded-full border border-brand-200 bg-white px-4 py-2 text-sm font-semibold text-brand-700 transition-colors group-hover:bg-brand-50">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4">
            <path d="M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z" />
            <circle cx="12" cy="10" r="2.5" />
          </svg>
          {t("cta")}
        </span>
      </div>
    </Link>
  );
}
