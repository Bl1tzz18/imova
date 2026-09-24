"use client";

import dynamic from "next/dynamic";
import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import type { Listing } from "@/types/listing";
import type { MapPoint } from "@/components/property/PropertyMapFull";

const PropertyMapFull = dynamic(
  () => import("@/components/property/PropertyMapFull").then((m) => m.PropertyMapFull),
  { ssr: false, loading: () => <div className="h-full w-full animate-pulse bg-ink-100" /> },
);

function ExpandIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d="M8 3H5a2 2 0 0 0-2 2v3m18-5h-3a2 2 0 0 0-2 2v3M3 16v3a2 2 0 0 0 2 2h3m11-5v3a2 2 0 0 1-2 2h-3" />
    </svg>
  );
}

function CloseIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className={className}>
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  );
}

function PinIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d="M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z" />
      <circle cx="12" cy="10" r="2.5" />
    </svg>
  );
}

function GoogleMapsLink({ lat, lng, label, className }: { lat: number; lng: number; label: string; className: string }) {
  return (
    <a
      href={`https://www.google.com/maps?q=${lat},${lng}`}
      target="_blank"
      rel="noopener noreferrer"
      className={className}
    >
      <PinIcon className="h-4 w-4" />
      {label}
    </a>
  );
}

// A single-pin reuse of the same Leaflet map PropertyMapExplorer uses for /map — no separate
// map-rendering setup needed for this one-marker preview on the listing detail page. Unlike
// /map (already a full-page explorer), this preview is small by default, so it gets its own
// expand-to-modal control; PropertyMapFull itself stays unaware of fullscreen state.
export function PropertyLocationPreview({ listing, lat, lng }: { listing: Listing; lat: number; lng: number }) {
  const t = useTranslations("PropertyDetail");
  const [isExpanded, setIsExpanded] = useState(false);
  const points: MapPoint[] = [{ listing, lat, lng }];

  const close = useCallback(() => setIsExpanded(false), []);

  useEffect(() => {
    if (!isExpanded) return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") close();
    };
    window.addEventListener("keydown", onKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [isExpanded, close]);

  return (
    <>
      <div className="relative isolate">
        <div className="h-56 overflow-hidden rounded-2xl border border-ink-100">
          <PropertyMapFull points={points} selectedId={listing.id} onSelect={() => {}} />
        </div>
        <button
          type="button"
          onClick={() => setIsExpanded(true)}
          aria-label={t("expandMap")}
          title={t("expandMap")}
          className="absolute right-2.5 top-2.5 z-[1000] flex h-9 w-9 items-center justify-center rounded-lg bg-white text-ink-700 shadow-md transition-colors hover:bg-ink-50"
        >
          <ExpandIcon className="h-4 w-4" />
        </button>
      </div>

      <GoogleMapsLink
        lat={lat}
        lng={lng}
        label={t("openInGoogleMaps")}
        className="mt-3 inline-flex h-10 items-center gap-2 rounded-full border border-ink-200 bg-white px-4 text-sm font-medium text-ink-900 transition-colors hover:border-ink-300 hover:bg-ink-50"
      />

      {isExpanded && (
        <div
          className="fixed inset-0 z-50 flex flex-col bg-black/70 p-4"
          role="dialog"
          aria-modal="true"
          aria-label={listing.title}
          onClick={close}
        >
          <div
            className="mx-auto flex w-full max-w-5xl flex-1 flex-col overflow-hidden rounded-2xl bg-white"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex shrink-0 items-center justify-between border-b border-ink-100 px-4 py-3">
              <GoogleMapsLink
                lat={lat}
                lng={lng}
                label={t("openInGoogleMaps")}
                className="inline-flex h-9 items-center gap-2 rounded-full border border-ink-200 px-3.5 text-sm font-medium text-ink-900 transition-colors hover:border-ink-300 hover:bg-ink-50"
              />
              <button
                type="button"
                onClick={close}
                aria-label={t("closeMap")}
                title={t("closeMap")}
                className="flex h-9 w-9 items-center justify-center rounded-full text-ink-500 transition-colors hover:bg-ink-100"
              >
                <CloseIcon className="h-4 w-4" />
              </button>
            </div>
            <div className="relative isolate flex-1">
              <PropertyMapFull points={points} selectedId={listing.id} onSelect={() => {}} />
            </div>
          </div>
        </div>
      )}
    </>
  );
}
