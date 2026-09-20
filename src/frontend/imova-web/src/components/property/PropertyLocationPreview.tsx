"use client";

import dynamic from "next/dynamic";
import type { Property } from "@/types/property";
import type { MapPoint } from "@/components/property/PropertyMapFull";

const PropertyMapFull = dynamic(
  () => import("@/components/property/PropertyMapFull").then((m) => m.PropertyMapFull),
  { ssr: false, loading: () => <div className="h-full w-full animate-pulse bg-ink-100" /> },
);

// A single-pin reuse of the same Leaflet map PropertyMapExplorer uses for /map — no separate
// map-rendering setup needed for this one-marker preview on the listing detail page.
export function PropertyLocationPreview({ property, lat, lng }: { property: Property; lat: number; lng: number }) {
  const points: MapPoint[] = [{ property, lat, lng }];

  return <PropertyMapFull points={points} selectedId={property.id} onSelect={() => {}} />;
}
