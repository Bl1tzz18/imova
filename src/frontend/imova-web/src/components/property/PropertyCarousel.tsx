"use client";

import { useRef } from "react";
import { useTranslations } from "next-intl";
import { PropertyCard } from "@/components/property/PropertyCard";
import type { Listing } from "@/types/listing";
import { cn } from "@/lib/utils/cn";

function scrollByPage(el: HTMLDivElement, direction: 1 | -1) {
  el.scrollBy({ left: direction * el.clientWidth * 0.9, behavior: "smooth" });
}

// One swipeable row of cards: most of a card plus a peek of the next on a phone (the "more this way"
// cue — the row runs to the screen edge, past the page's 16 px gutter), two per view on a tablet, and three — or four, for a full-width row like the home page's —
// on a desktop, with arrows.
export function PropertyCarousel({ listings, desktopPerView = 3 }: { listings: Listing[]; desktopPerView?: 3 | 4 }) {
  const tCommon = useTranslations("Common");
  const scrollerRef = useRef<HTMLDivElement>(null);

  return (
    <div className="relative">
      <div
        ref={scrollerRef}
        className="-mx-4 flex snap-x snap-mandatory scroll-px-4 gap-5 overflow-x-auto scroll-smooth px-4 pb-2 sm:mx-0 sm:scroll-px-0 sm:px-0 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {listings.map((listing) => (
          <div
            key={listing.id}
            className={cn(
              "shrink-0 snap-start basis-[85%] sm:basis-[calc((100%-1.25rem)/2)]",
              desktopPerView === 4 ? "lg:basis-[calc((100%-3.75rem)/4)]" : "lg:basis-[calc((100%-2.5rem)/3)]",
            )}
          >
            <PropertyCard listing={listing} />
          </div>
        ))}
      </div>

      <button
        type="button"
        onClick={() => scrollerRef.current && scrollByPage(scrollerRef.current, -1)}
        aria-label={tCommon("previous")}
        className="absolute left-0 top-[38%] hidden -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full border border-ink-100 bg-white p-3.5 text-ink-700 shadow-[var(--shadow-card-hover)] transition-colors hover:bg-ink-50 sm:flex"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-6 w-6">
          <path d="M15 18l-6-6 6-6" />
        </svg>
      </button>
      <button
        type="button"
        onClick={() => scrollerRef.current && scrollByPage(scrollerRef.current, 1)}
        aria-label={tCommon("next")}
        className="absolute right-0 top-[38%] hidden translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full border border-ink-100 bg-white p-3.5 text-ink-700 shadow-[var(--shadow-card-hover)] transition-colors hover:bg-ink-50 sm:flex"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-6 w-6">
          <path d="M9 18l6-6-6-6" />
        </svg>
      </button>
    </div>
  );
}
