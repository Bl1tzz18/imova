"use client";

import { useRef } from "react";
import { useTranslations } from "next-intl";
import { PropertyCard } from "@/components/property/PropertyCard";
import type { Property } from "@/types/property";

function scrollByPage(el: HTMLDivElement, direction: 1 | -1) {
  el.scrollBy({ left: direction * el.clientWidth * 0.9, behavior: "smooth" });
}

export function PropertyCarousel({ properties }: { properties: Property[] }) {
  const tCommon = useTranslations("Common");
  const scrollerRef = useRef<HTMLDivElement>(null);

  return (
    <div className="relative">
      <div
        ref={scrollerRef}
        className="flex snap-x snap-mandatory gap-5 overflow-x-auto scroll-smooth pb-2 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {properties.map((property) => (
          <div
            key={property.id}
            className="shrink-0 snap-start basis-[85%] sm:basis-[calc((100%-1.25rem)/2)] lg:basis-[calc((100%-2.5rem)/3)]"
          >
            <PropertyCard property={property} />
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
