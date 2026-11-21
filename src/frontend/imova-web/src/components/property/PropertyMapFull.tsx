"use client";

import "leaflet/dist/leaflet.css";
import "leaflet.markercluster/dist/MarkerCluster.css";
import { useEffect, useRef } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { MapContainer, Marker, Popup, TileLayer, useMap, useMapEvent } from "react-leaflet";
import MarkerClusterGroup from "react-leaflet-cluster";
import { point, type LeafletMouseEvent, type MarkerCluster } from "leaflet";
import { clusterClickZoom } from "@/lib/map/clusterZoom";
import { createClusterIcon, createPropertyMarkerIcon, createPropertyPricePinIcon } from "@/lib/map/propertyMarkerIcon";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import { coverPhoto } from "@/lib/listing/view";
import type { Listing } from "@/types/listing";

const MOLDOVA_CENTER: [number, number] = [47.1, 28.6];

export type MapPoint = { listing: Listing; lat: number; lng: number };

function FlyToSelected({ point }: { point: MapPoint | null }) {
  const map = useMap();

  useEffect(() => {
    if (point) {
      map.flyTo([point.lat, point.lng], Math.max(map.getZoom(), 12), { duration: 0.6 });
    }
  }, [point, map]);

  return null;
}

function PropertyPopupContent({ listing }: { listing: Listing }) {
  const t = useTranslations("MapPage");
  const location = formatLocation(listing.property.location);
  const cover = coverPhoto(listing);

  return (
    <Link href={`/property/${listing.id}`} className="flex w-52 flex-col gap-1">
      <div className="-mx-3 -mt-3 mb-1 flex aspect-[16/10] items-center justify-center overflow-hidden bg-brand-800">
        {cover ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={cover.cardUrl} alt={listing.title} className="h-full w-full object-cover" />
        ) : null}
      </div>
      <p className="text-sm font-semibold text-ink-950">{formatPrice(listing.price.amount, listing.price.currency)}</p>
      <p className="line-clamp-2 text-xs text-ink-700">{listing.title}</p>
      {location && <p className="text-xs text-ink-400">{location}</p>}
      <span className="mt-1 text-xs font-semibold text-brand-700">{t("viewListing")}</span>
    </Link>
  );
}

function PropertyMarkers({ points, selectedId, onSelect }: { points: MapPoint[]; selectedId: string | null; onSelect: (id: string) => void }) {
  return points.map((point) => (
    <Marker
      key={point.listing.id}
      position={[point.lat, point.lng]}
      icon={createPropertyMarkerIcon(point.listing.id === selectedId)}
      eventHandlers={{ click: () => onSelect(point.listing.id) }}
    >
      <Popup>
        <PropertyPopupContent listing={point.listing} />
      </Popup>
    </Marker>
  ));
}

// Keyed to 6 decimals (~0.1m) so markers at the exact same or near-identical coordinates
// (e.g. several units in the same building) match up reliably despite float rounding.
function pointKey(lat: number, lng: number) {
  return `${lat.toFixed(6)},${lng.toFixed(6)}`;
}

// The zooms maxClusterRadius steps down at (see clusterRadiusForZoom) — roughly 3 cluster
// "tiers" instead of a smooth/gradual breakdown across every zoom level: wide (city/region,
// below tier[0]), medium (neighborhood/street, between the two), and tight (building/point at
// maxZoom — only markers sharing the same or a near-identical coordinate still merge there).
// Deliberately not using `disableClusteringAtZoom`: that would stop clustering outright beyond a
// fixed zoom, including at maxZoom, which would break the "still a cluster at maxZoom → show
// every listing at that point in the overflow panel" feature for listings sharing one exact
// coordinate.
const ZOOM_TIERS = [13, 18];

// Room kept around a clicked cluster's listings when zooming to them, in px (both sides together),
// so their price pills aren't cut off at the map's edge.
const CLUSTER_FIT_PADDING = 120;

function clusterRadiusForZoom(zoom: number) {
  if (zoom < ZOOM_TIERS[0]) return 100;
  if (zoom < ZOOM_TIERS[1]) return 50;
  return 20;
}

// /map explorer only: groups nearby listings into a count cluster, breaking apart into smaller
// clusters or individual price pills as the user zooms in. A click on a cluster zooms to its own
// listings — centred on them, as close as still shows all of them (clusterClickZoom), at least one
// level in. (It used to jump to a fixed zoom around the click point, which could leave every pin of
// a spread-out cluster off screen.) When zooming can't separate them — already at the max zoom, or
// every listing on the very same spot — the click reports them via `onClusterOverflow` instead,
// for the caller to show in its "same address" panel (MapSpotPanel).
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
        const cluster = (e as unknown as { layer: MarkerCluster }).layer;
        const childKeys = new Set(cluster.getAllChildMarkers().map((m) => pointKey(m.getLatLng().lat, m.getLatLng().lng)));
        const maxZoom = map.getMaxZoom();
        if (map.getZoom() >= maxZoom || childKeys.size === 1) {
          if (map.getZoom() < maxZoom) map.setView(cluster.getLatLng(), maxZoom);
          onClusterOverflow(points.filter((p) => childKeys.has(pointKey(p.lat, p.lng))));
          return;
        }
        onClusterOverflow(null);
        const bounds = cluster.getBounds();
        const fitZoom = map.getBoundsZoom(bounds, false, point(CLUSTER_FIT_PADDING, CLUSTER_FIT_PADDING));
        map.setView(bounds.getCenter(), clusterClickZoom(map.getZoom(), fitZoom, maxZoom));
      }}
    >
      {points.map((point) => (
        <Marker
          key={point.listing.id}
          position={[point.lat, point.lng]}
          icon={createPropertyPricePinIcon(formatPrice(point.listing.price.amount, point.listing.price.currency), point.listing.id === selectedId)}
          eventHandlers={{ click: () => onSelect(point.listing.id) }}
        >
          <Popup>
            <PropertyPopupContent listing={point.listing} />
          </Popup>
        </Marker>
      ))}
    </MarkerClusterGroup>
  );
}

// Frames the pins whenever the set of listings changes (a new filter, not a selection or a
// re-render with the same results): one pin → a street-level view of it, several → all of them in
// view. The search map opts in, so a filter for one sector shows that sector, not the whole country.
function FitToPoints({ points }: { points: MapPoint[] }) {
  const map = useMap();
  const latest = useRef(points);
  latest.current = points;
  const key = points.map((p) => p.listing.id).join(",");

  useEffect(() => {
    const current = latest.current;
    if (current.length === 0) return;
    if (current.length === 1) {
      map.setView([current[0].lat, current[0].lng], 14);
      return;
    }
    map.fitBounds(
      current.map((p) => [p.lat, p.lng] as [number, number]),
      { padding: [48, 48], maxZoom: 15 },
    );
  }, [map, key]);

  return null;
}

export function PropertyMapFull({
  points,
  selectedId,
  onSelect,
  cluster = false,
  onClusterOverflow,
  fitToPoints = false,
}: {
  points: MapPoint[];
  selectedId: string | null;
  onSelect: (id: string) => void;
  cluster?: boolean;
  onClusterOverflow?: (points: MapPoint[] | null) => void;
  fitToPoints?: boolean;
}) {
  const selectedPoint = points.find((point) => point.listing.id === selectedId) ?? null;

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
      {fitToPoints && <FitToPoints points={points} />}
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
