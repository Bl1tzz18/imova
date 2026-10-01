import { PropertyCarousel } from "@/components/property/PropertyCarousel";
import type { Listing } from "@/types/listing";

// "Anunțuri asemănătoare" at the end of a listing page — the backend picks them (same transaction
// and property type, nearest area and price first). Nothing at all when there are none: no heading
// over an empty row.
export function SimilarListings({ listings, title }: { listings: Listing[]; title: string }) {
  if (listings.length === 0) {
    return null;
  }

  return (
    <section className="mt-12" aria-labelledby="similar-listings-title">
      <h2 id="similar-listings-title" className="font-display text-xl font-medium text-ink-950">
        {title}
      </h2>
      <div className="mt-4">
        <PropertyCarousel listings={listings} />
      </div>
    </section>
  );
}
