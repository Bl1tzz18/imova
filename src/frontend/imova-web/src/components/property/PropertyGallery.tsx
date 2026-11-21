"use client";

import { useCallback, useMemo, useRef, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { ImageViewer } from "@/components/ui/ImageViewer";
import { photoSrcSet } from "@/lib/listing/view";
import type { Photo } from "@/types/listing";

// Hero image + thumbnail strip on the property detail page, backed by the full-screen ImageViewer
// for stepping through every photo at full size. Closing it returns focus to the photo that opened
// it. Client-only because it needs click/keyboard state — the page itself stays a Server Component
// and passes the media down.
export function PropertyGallery({
  media,
  title,
  propertyType,
  actions,
}: {
  media: Photo[];
  title: string;
  propertyType: string;
  // Round buttons in the main photo's top-right corner (share, save) — beside the photo's own
  // button, not inside it, so tapping them never opens the gallery.
  actions?: ReactNode;
}) {
  const [openIndex, setOpenIndex] = useState<number | null>(null);
  const t = useTranslations("PropertyDetail");
  const opener = useRef<HTMLElement | null>(null);

  const open = useCallback((index: number, from: HTMLElement) => {
    opener.current = from;
    setOpenIndex(index);
  }, []);
  const close = useCallback(() => {
    setOpenIndex(null);
    opener.current?.focus();
  }, []);
  const viewerImages = useMemo(
    () => media.map((p) => ({ id: p.id, src: p.url, thumbnailSrc: p.thumbnailUrl })),
    [media]
  );

  const corner = actions && <div className="absolute right-3 top-3 z-10 flex gap-2">{actions}</div>;

  if (media.length === 0) {
    return (
      <div className="relative">
        <div className="flex aspect-[4/3] items-center justify-center overflow-hidden rounded-2xl border border-ink-100 bg-gradient-to-br from-brand-800 to-brand-600 sm:aspect-[16/9]">
          <PropertyIcon type={propertyType} className="h-20 w-20 text-white/25 sm:h-28 sm:w-28" />
        </div>
        {corner}
      </div>
    );
  }

  return (
    <>
      <div className="relative">
        <button
          type="button"
          onClick={(e) => open(0, e.currentTarget)}
          aria-label={t("openGallery", { count: media.length })}
          className="group block w-full cursor-zoom-in overflow-hidden rounded-2xl border border-ink-100 bg-gradient-to-br from-brand-800 to-brand-600"
        >
          <div className="relative aspect-[4/3] sm:aspect-[16/9]">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={media[0].url}
              srcSet={photoSrcSet(media[0], "large")}
              sizes="(min-width: 1024px) 800px, 100vw"
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
        {corner}
      </div>

      {media.length > 1 && (
        <div className="mt-3 grid grid-cols-4 gap-3 sm:grid-cols-6">
          {media.slice(1).map((item, i) => (
            <button
              key={item.id}
              type="button"
              onClick={(e) => open(i + 1, e.currentTarget)}
              aria-label={t("photoNumber", { number: i + 2, total: media.length })}
              className="aspect-square cursor-zoom-in overflow-hidden rounded-xl border border-ink-100"
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={item.thumbnailUrl}
                alt=""
                loading="lazy"
                className="h-full w-full object-cover transition-transform hover:scale-105"
              />
            </button>
          ))}
        </div>
      )}

      {openIndex !== null && (
        <ImageViewer
          images={viewerImages}
          index={openIndex}
          onIndexChange={setOpenIndex}
          onClose={close}
          alt={title}
          labels={{
            close: t("closeGallery"),
            previous: t("previousPhoto"),
            next: t("nextPhoto"),
            counter: (current, total) => t("photoCounter", { current, total }),
            imageNumber: (number, total) => t("photoNumber", { number, total }),
          }}
        />
      )}
    </>
  );
}
