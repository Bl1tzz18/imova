"use client";

import { useCallback, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { swipeDirection } from "@/lib/listing/swipe";

export type ViewerImage = { id: string; src: string; thumbnailSrc: string };

export type ImageViewerLabels = {
  close: string;
  previous: string;
  next: string;
  counter: (current: number, total: number) => string;
  imageNumber: (number: number, total: number) => string;
};

function ChevronIcon({ direction, className }: { direction: "left" | "right"; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <path d={direction === "left" ? "m15 18-6-6 6-6" : "m9 18 6-6-6-6"} />
    </svg>
  );
}

// Full-screen lightbox for stepping through images at full size — arrows, keyboard (← → Esc), a
// strip of thumbnails and, on a phone, swiping; a tap on the dark backdrop closes it. Focus moves to
// the close button while it's open (the caller puts it back on whatever opened it, in onClose), the
// page behind doesn't scroll, and the images either side of the current one are preloaded. Rendered
// into <body>, so wherever the opener sits (a message list that is a live region, a container with a
// transform) the overlay still covers the whole screen and isn't read out as page content. Used by
// the listing gallery and by message images.
export function ImageViewer({
  images,
  index,
  onIndexChange,
  onClose,
  alt,
  labels,
}: {
  images: ViewerImage[];
  index: number;
  onIndexChange: (index: number) => void;
  onClose: () => void;
  // The dialog's name and the full-size image's alt text.
  alt: string;
  labels: ImageViewerLabels;
}) {
  const closeButton = useRef<HTMLButtonElement>(null);
  const touchStart = useRef<{ x: number; y: number } | null>(null);
  const count = images.length;

  const showPrev = useCallback(() => onIndexChange((index - 1 + count) % count), [index, count, onIndexChange]);
  const showNext = useCallback(() => onIndexChange((index + 1) % count), [index, count, onIndexChange]);

  useEffect(() => {
    closeButton.current?.focus();
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, []);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (count < 2) return;
      if (e.key === "ArrowLeft") showPrev();
      if (e.key === "ArrowRight") showNext();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [count, onClose, showPrev, showNext]);

  const current = images[index];
  if (!current) return null;

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex flex-col bg-black/95 p-4"
      role="dialog"
      aria-modal="true"
      aria-label={alt}
      onClick={onClose}
      onTouchStart={(e) => {
        const touch = e.touches[0];
        touchStart.current = touch ? { x: touch.clientX, y: touch.clientY } : null;
      }}
      onTouchEnd={(e) => {
        const start = touchStart.current;
        const touch = e.changedTouches[0];
        touchStart.current = null;
        if (!start || !touch || count < 2) return;
        const direction = swipeDirection(touch.clientX - start.x, touch.clientY - start.y);
        if (direction === "next") showNext();
        if (direction === "previous") showPrev();
      }}
    >
      <div className="flex shrink-0 items-center justify-between px-2 text-sm text-white/70">
        <span>{count > 1 ? labels.counter(index + 1, count) : null}</span>
        <button
          ref={closeButton}
          type="button"
          onClick={onClose}
          aria-label={labels.close}
          className="flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-5 w-5">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </button>
      </div>

      <div className="relative flex min-h-0 flex-1 items-center justify-center overflow-hidden">
        {count > 1 && (
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              showPrev();
            }}
            aria-label={labels.previous}
            className="absolute left-0 z-10 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20 sm:left-4 sm:h-12 sm:w-12"
          >
            <ChevronIcon direction="left" className="h-5 w-5" />
          </button>
        )}

        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={current.src}
          alt={alt}
          className="max-h-full max-w-full select-none object-contain"
          draggable={false}
          onClick={(e) => e.stopPropagation()}
        />
        {/* The neighbours, loaded ahead so the next swipe shows its image at once. */}
        {count > 1 &&
          [images[(index + 1) % count], images[(index - 1 + count) % count]].map((p) => (
            // eslint-disable-next-line @next/next/no-img-element
            <img key={p.id} src={p.src} alt="" aria-hidden className="hidden" />
          ))}

        {count > 1 && (
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              showNext();
            }}
            aria-label={labels.next}
            className="absolute right-0 z-10 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20 sm:right-4 sm:h-12 sm:w-12"
          >
            <ChevronIcon direction="right" className="h-5 w-5" />
          </button>
        )}
      </div>

      {count > 1 && (
        <div className="flex shrink-0 justify-center gap-2 overflow-x-auto px-2 pt-3">
          {images.map((item, i) => (
            <button
              key={item.id}
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                onIndexChange(i);
              }}
              aria-label={labels.imageNumber(i + 1, count)}
              aria-current={i === index ? "true" : undefined}
              className={`h-12 w-16 shrink-0 overflow-hidden rounded-lg border-2 transition-opacity ${
                i === index ? "border-white opacity-100" : "border-transparent opacity-50 hover:opacity-80"
              }`}
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={item.thumbnailSrc} alt="" className="h-full w-full object-cover" />
            </button>
          ))}
        </div>
      )}
    </div>,
    document.body
  );
}
