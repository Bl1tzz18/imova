"use client";

import "leaflet/dist/leaflet.css";
import "leaflet.markercluster/dist/MarkerCluster.css";
import { useEffect } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { MapContainer, Marker, Popup, TileLayer, useMap, useMapEvent } from "react-leaflet";
import MarkerClusterGroup from "react-leaflet-cluster";
import type { LeafletMouseEvent, MarkerCluster } from "leaflet";
import { createClusterIcon, createPropertyMarkerIcon, createPropertyPricePinIcon } from "@/lib/map/propertyMarkerIcon";
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

function PropertyMarkers({ points, selectedId, onSelect }: { points: MapPoint[]; selectedId: string | null; onSelect: (id: string) => void }) {
  return points.map((point) => (
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
  ));
}

// Keyed to 6 decimals (~0.1m) so markers at the exact same or near-identical coordinates
// (e.g. several units in the same building) match up reliably despite float rounding.
function pointKey(lat: number, lng: number) {
  return `${lat.toFixed(6)},${lng.toFixed(6)}`;
}

// Zoom levels a cluster click jumps to (see ClusteredPropertyMarkers' onClick below) and the
// boundaries maxClusterRadius steps down at (see clusterRadiusForZoom) — roughly 3 cluster
// "tiers" instead of a smooth/gradual breakdown across every zoom level: wide (city/region,
// below tier[0]), medium (neighborhood/street, between the two), and tight (building/point at
// maxZoom — only markers sharing the same or a near-identical coordinate still merge there).
// Deliberately not using `disableClusteringAtZoom`: that would stop clustering outright beyond a
// fixed zoom, including at maxZoom, which would break the "still a cluster at maxZoom → show
// every listing at that point in the overflow panel" feature for listings sharing one exact
// coordinate.
const ZOOM_TIERS = [13, 18];

function clusterRadiusForZoom(zoom: number) {
  if (zoom < ZOOM_TIERS[0]) return 100;
  if (zoom < ZOOM_TIERS[1]) return 50;
  return 20;
}

function nextTierZoom(currentZoom: number, maxZoom: number) {
  const next = ZOOM_TIERS.find((zoom) => zoom > currentZoom);
  return Math.min(next ?? maxZoom, maxZoom);
}

// /map explorer only: groups nearby listings into a count cluster, breaking apart into smaller
// clusters or individual price pills as the user zooms in. `zoomToBoundsOnClick` is turned off
// (its default "fit bounds of this cluster's children" jump can be unpredictable — anywhere from
// barely zooming to leaping straight to individual markers) in favor of a custom handler that
// jumps to the next zoom tier, so a couple of clicks gets from a city-wide view down to
// individual listings instead of needing many one-level-at-a-time clicks. Once already at the
// map's max zoom, stepping in further is impossible — if it's still a cluster at that point, its
// markers must share the same (or a near-identical) coordinate and can never separate by zooming
// alone, so the click instead reports the full list of listings at that point via
// `onClusterOverflow`, for the caller to show as a side panel.
function ClusteredPropertyMarkers({
  points,
  selectedId,
  onSelect,
  onClusterOverflow,
}: {
  points: MapPoint[];
  selectedId: string | null;
  onSelect: (id: string) => void;
  onClusterOverflow: (points: MapPoint[] | null) => void;
}) {
  const map = useMap();

  useMapEvent("click", () => onClusterOverflow(null));

  return (
    <MarkerClusterGroup
      maxClusterRadius={clusterRadiusForZoom}
      showCoverageOnHover={false}
      spiderfyOnMaxZoom={false}
      zoomToBoundsOnClick={false}
      iconCreateFunction={createClusterIcon}
      onClick={(e: LeafletMouseEvent) => {
        if (map.getZoom() >= map.getMaxZoom()) {
          const cluster = (e as unknown as { layer: MarkerCluster }).layer;
          const childKeys = new Set(cluster.getAllChildMarkers().map((m) => pointKey(m.getLatLng().lat, m.getLatLng().lng)));
          onClusterOverflow(points.filter((p) => childKeys.has(pointKey(p.lat, p.lng))));
          return;
        }
        onClusterOverflow(null);
        map.setView(e.latlng, nextTierZoom(map.getZoom(), map.getMaxZoom()));
      }}
    >
      {points.map((point) => (
        <Marker
          key={point.property.id}
          position={[point.lat, point.lng]}
          icon={createPropertyPricePinIcon(formatPrice(point.property.price, point.property.currency), point.property.id === selectedId)}
          eventHandlers={{ click: () => onSelect(point.property.id) }}
        >
          <Popup>
            <PropertyPopupContent property={point.property} />
          </Popup>
        </Marker>
      ))}
    </MarkerClusterGroup>
  );
}

export function PropertyMapFull({
  points,
  selectedId,
  onSelect,
  cluster = false,
  onClusterOverflow,
}: {
  points: MapPoint[];
  selectedId: string | null;
  onSelect: (id: string) => void;
  cluster?: boolean;
  onClusterOverflow?: (points: MapPoint[] | null) => void;
}) {
  const selectedPoint = points.find((point) => point.property.id === selectedId) ?? null;

  return (
    <MapContainer
      center={MOLDOVA_CENTER}
      zoom={8}
      className="h-full w-full"
      attributionControl={false}
      zoomControl
      dragging
      scrollWheelZoom
      doubleClickZoom
      touchZoom
    >
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
      <FlyToSelected point={selectedPoint} />
      {cluster ? (
        <ClusteredPropertyMarkers
          points={points}
          selectedId={selectedId}
          onSelect={onSelect}
          onClusterOverflow={onClusterOverflow ?? (() => {})}
        />
      ) : (
        <PropertyMarkers points={points} selectedId={selectedId} onSelect={onSelect} />
      )}
    </MapContainer>
  );
}
