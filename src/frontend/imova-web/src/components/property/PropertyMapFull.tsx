"use client";

import "leaflet/dist/leaflet.css";
import { useEffect } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet";
import { createPropertyMarkerIcon } from "@/lib/map/propertyMarkerIcon";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import type { Property } from "@/types/property";

const MOLDOVA_CENTER: [number, number] = [47.1, 28.6];

export type MapPoint = { property: Property; lat: number; lng: number };

function FlyToSelected({ point }: { point: MapPoint | null }) {
  const map = useMap();

  useEffect(() => {
    if (point) {
      map.flyTo([point.lat, point.lng], Math.max(map.getZoom(), 12), { duration: 0.6 });
    }
  }, [point, map]);

  return null;
}

function PropertyPopupContent({ property }: { property: Property }) {
  const t = useTranslations("MapPage");
  const location = formatLocation(property.location);

  return (
    <Link href={`/property/${property.id}`} className="flex w-52 flex-col gap-1">
      <div className="-mx-3 -mt-3 mb-1 flex aspect-[16/10] items-center justify-center overflow-hidden bg-brand-800">
        {property.media[0] ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={property.media[0].url} alt={property.title} className="h-full w-full object-cover" />
        ) : null}
      </div>
      <p className="text-sm font-semibold text-ink-950">{formatPrice(property.price, property.currency)}</p>
      <p className="line-clamp-2 text-xs text-ink-700">{property.title}</p>
      {location && <p className="text-xs text-ink-400">{location}</p>}
      <span className="mt-1 text-xs font-semibold text-brand-700">{t("viewListing")}</span>
    </Link>
  );
}

export function PropertyMapFull({
  points,
  selectedId,
  onSelect,
}: {
  points: MapPoint[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}) {
  const selectedPoint = points.find((point) => point.property.id === selectedId) ?? null;

  return (
    <MapContainer center={MOLDOVA_CENTER} zoom={8} className="h-full w-full" attributionControl={false}>
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
      <FlyToSelected point={selectedPoint} />
      {points.map((point) => (
        <Marker
          key={point.property.id}
          position={[point.lat, point.lng]}
          icon={createPropertyMarkerIcon(point.property.id === selectedId)}
          eventHandlers={{ click: () => onSelect(point.property.id) }}
        >
          <Popup>
            <PropertyPopupContent property={point.property} />
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
}
