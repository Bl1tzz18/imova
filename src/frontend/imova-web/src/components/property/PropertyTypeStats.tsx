import Link from "next/link";
import { getLocale, getTranslations } from "next-intl/server";
import { searchHref } from "@/lib/search/filters";
import type { Listing } from "@/types/listing";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;

export async function PropertyTypeStats({ listings }: { listings: Listing[] }) {
  const locale = await getLocale();
  const t = await getTranslations("Search");
  const formatter = new Intl.NumberFormat(locale);

  const counts = PROPERTY_TYPES.map((type) => ({
    type,
    label: t(`typePlural.${type}`),
    count: listings.filter((p) => p.property.propertyType === type).length,
  }));

  return (
    <div className="grid grid-cols-2 gap-px overflow-hidden rounded-2xl border border-ink-100 bg-ink-100 shadow-[var(--shadow-card-hover)] sm:grid-cols-3 lg:grid-cols-6">
      {counts.map((item) => (
        <Link
          key={item.type}
          // Straight to that type's results — no Sale/Rent preset; the filters can narrow it.
          href={searchHref({ propertyType: [item.type] })}
          className="flex flex-col items-center justify-center gap-1.5 bg-white px-4 py-6 text-center transition-colors hover:bg-ink-50 sm:py-7"
        >
          <span className="text-[11px] font-medium uppercase tracking-widest text-ink-500">
            {item.label}
          </span>
          <span className="font-display text-2xl font-semibold text-ink-950 sm:text-3xl">
            {formatter.format(item.count)}
          </span>
        </Link>
      ))}
    </div>
  );
}
