"use client";

import dynamic from "next/dynamic";
import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { cn } from "@/lib/utils/cn";
import { formatPrice } from "@/lib/utils/format";
import type { Property } from "@/types/property";
import type { MapPoint } from "@/components/property/PropertyMapFull";

const PropertyMapFull = dynamic(
  () => import("@/components/property/PropertyMapFull").then((m) => m.PropertyMapFull),
  { ssr: false, loading: () => <div className="h-full w-full animate-pulse bg-ink-100" /> },
);

export function PropertyMapExplorer({ properties }: { properties: Property[] }) {
  const t = useTranslations("MapPage");
  const tType = useTranslations("PropertyType");
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const points = useMemo<MapPoint[]>(
    () =>
      properties
        .filter((property) => property.location)
        .map((property) => ({ property, lat: property.location!.latitude, lng: property.location!.longitude })),
    [properties],
  );

  return (
    <div className="mx-auto max-w-[100rem] px-4 py-6 sm:px-6 sm:py-8">
      <div className="flex h-[88vh] min-h-[680px] flex-col overflow-hidden rounded-3xl border border-ink-100 bg-white shadow-[var(--shadow-card)] lg:flex-row">
        <aside className="order-2 flex w-full flex-col overflow-y-auto border-ink-100 bg-white lg:order-1 lg:h-full lg:w-96 lg:shrink-0 lg:border-r">
          <div className="border-b border-ink-100 px-5 py-4">
            <h1 className="font-display text-lg font-medium text-ink-950">{t("title")}</h1>
            <p className="mt-0.5 text-sm text-ink-500">
              {points.length > 0 ? t("resultsCount", { count: points.length }) : t("noResults")}
            </p>
          </div>

          <ul className="divide-y divide-ink-100">
            {points.map(({ property }) => (
              <li key={property.id}>
                <button
                  type="button"
                  onClick={() => setSelectedId(property.id)}
                  className={cn(
                    "flex w-full items-center gap-3 px-5 py-3 text-left transition-colors hover:bg-ink-50",
                    selectedId === property.id && "bg-brand-50",
                  )}
                >
                  <span className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-brand-800">
                    {property.media[0] ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={property.media[0].url} alt="" className="h-full w-full object-cover" />
                    ) : (
                      <PropertyIcon type={property.propertyType} className="h-6 w-6 text-white/50" />
                    )}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-medium text-ink-900">{property.title}</span>
                    <span className="block text-xs text-ink-500">
                      {formatPrice(property.price, property.currency)} · {tType(property.propertyType)}
                    </span>
                  </span>
                </button>
              </li>
            ))}
          </ul>
        </aside>

        <div className="order-1 h-72 shrink-0 lg:order-2 lg:h-full lg:flex-1">
          <PropertyMapFull points={points} selectedId={selectedId} onSelect={setSelectedId} />
        </div>
      </div>
    </div>
  );
}
