"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import type { Photo } from "@/types/listing";

function ChevronIcon({ direction, className }: { direction: "left" | "right"; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d={direction === "left" ? "m15 18-6-6 6-6" : "m9 18 6-6-6-6"} />
    </svg>
  );
}

// Hero image + thumbnail strip on the property detail page, backed by a full-screen lightbox
// for stepping through every photo at full size. Client-only because it needs click/keyboard
// state — the page itself stays a Server Component and just passes the fetched media down.
export function PropertyGallery({
  media,
  title,
  propertyType,
}: {
  media: Photo[];
  title: string;
  propertyType: string;
}) {
  const [openIndex, setOpenIndex] = useState<number | null>(null);
  const t = useTranslations("PropertyDetail");

  const close = useCallback(() => setOpenIndex(null), []);
  const showPrev = useCallback(
    () => setOpenIndex((i) => (i === null ? null : (i - 1 + media.length) % media.length)),
    [media.length]
  );
  const showNext = useCallback(
    () => setOpenIndex((i) => (i === null ? null : (i + 1) % media.length)),
    [media.length]
  );

  useEffect(() => {
    if (openIndex === null) return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") close();
      if (e.key === "ArrowLeft") showPrev();
      if (e.key === "ArrowRight") showNext();
    };
    window.addEventListener("keydown", onKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [openIndex, close, showPrev, showNext]);

  if (media.length === 0) {
    return (
      <div className="mt-4 overflow-hidden rounded-2xl border border-ink-100 bg-gradient-to-br from-brand-800 to-brand-600">
        <div className="flex aspect-[21/9] items-center justify-center">
          <PropertyIcon type={propertyType} className="h-20 w-20 text-white/25 sm:h-28 sm:w-28" />
        </div>
      </div>
    );
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setOpenIndex(0)}
        className="group mt-4 block w-full cursor-zoom-in overflow-hidden rounded-2xl border border-ink-100 bg-gradient-to-br from-brand-800 to-brand-600"
      >
        <div className="relative aspect-[21/9]">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={media[0].url}
            alt={title}
            className="absolute inset-0 h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.03]"
          />
          {media.length > 1 && (
            <span className="absolute bottom-3 right-3 rounded-full bg-black/60 px-3 py-1 text-xs font-medium text-white backdrop-blur">
              {t("photoCount", { count: media.length })}
            </span>
          )}
        </div>
      </button>

      {media.length > 1 && (
        <div className="mt-3 grid grid-cols-4 gap-3 sm:grid-cols-6">
          {media.slice(1).map((item, i) => (
            <button
              key={item.id}
              type="button"
              onClick={() => setOpenIndex(i + 1)}
              className="aspect-square cursor-zoom-in overflow-hidden rounded-xl border border-ink-100"
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={item.url}
                alt={title}
                loading="lazy"
                className="h-full w-full object-cover transition-transform hover:scale-105"
              />
            </button>
          ))}
        </div>
      )}

      {openIndex !== null && (
        <div
          className="fixed inset-0 z-50 flex flex-col bg-black/95 p-4"
          role="dialog"
          aria-modal="true"
          aria-label={title}
          onClick={close}
        >
          <div className="flex shrink-0 items-center justify-between px-2 text-sm text-white/70">
            <span>{t("photoCounter", { current: openIndex + 1, total: media.length })}</span>
            <button
              type="button"
              onClick={close}
              aria-label={t("closeGallery")}
              className="flex h-9 w-9 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-4 w-4">
                <path d="M6 6l12 12M18 6L6 18" />
              </svg>
            </button>
          </div>

          <div className="relative flex flex-1 items-center justify-center overflow-hidden">
            {media.length > 1 && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  showPrev();
                }}
                aria-label={t("previousPhoto")}
                className="absolute left-0 z-10 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20 sm:left-4 sm:h-12 sm:w-12"
              >
                <ChevronIcon direction="left" className="h-5 w-5" />
              </button>
            )}

            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={media[openIndex].url}
              alt={title}
              className="max-h-full max-w-full object-contain"
              onClick={(e) => e.stopPropagation()}
            />

            {media.length > 1 && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  showNext();
                }}
                aria-label={t("nextPhoto")}
                className="absolute right-0 z-10 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20 sm:right-4 sm:h-12 sm:w-12"
              >
                <ChevronIcon direction="right" className="h-5 w-5" />
              </button>
            )}
          </div>

          {media.length > 1 && (
            <div className="flex shrink-0 justify-center gap-2 overflow-x-auto px-2 pt-3">
              {media.map((item, i) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    setOpenIndex(i);
                  }}
                  className={`h-12 w-16 shrink-0 overflow-hidden rounded-lg border-2 transition-opacity ${
                    i === openIndex ? "border-white opacity-100" : "border-transparent opacity-50 hover:opacity-80"
                  }`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={item.url} alt="" className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </>
  );
}
