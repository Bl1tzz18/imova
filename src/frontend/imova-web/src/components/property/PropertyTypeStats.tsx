import { getLocale, getTranslations } from "next-intl/server";
import type { Property } from "@/types/property";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;

export async function PropertyTypeStats({ properties }: { properties: Property[] }) {
  const locale = await getLocale();
  const t = await getTranslations("PropertyType");
  const formatter = new Intl.NumberFormat(locale);

  const counts = PROPERTY_TYPES.map((type) => ({
    type,
    label: t(type),
    count: properties.filter((p) => p.propertyType === type).length,
  }));

  return (
    <div className="grid grid-cols-2 gap-px overflow-hidden rounded-2xl border border-ink-100 bg-ink-100 shadow-[var(--shadow-card-hover)] sm:grid-cols-3 lg:grid-cols-6">
      {counts.map((item) => (
        <div
          key={item.type}
          className="flex flex-col items-center justify-center gap-1.5 bg-white px-4 py-6 text-center sm:py-7"
        >
          <span className="text-[11px] font-medium uppercase tracking-widest text-ink-500">
            {item.label}
          </span>
          <span className="font-display text-2xl font-semibold text-ink-950 sm:text-3xl">
            {formatter.format(item.count)}
          </span>
        </div>
      ))}
    </div>
  );
}
