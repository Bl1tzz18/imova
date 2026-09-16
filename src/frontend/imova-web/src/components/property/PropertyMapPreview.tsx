"use client";

import "leaflet/dist/leaflet.css";
import { MapContainer, Marker, TileLayer } from "react-leaflet";
import { createPropertyMarkerIcon } from "@/lib/map/propertyMarkerIcon";
import type { Property } from "@/types/property";

const MOLDOVA_CENTER: [number, number] = [47.1, 28.6];

// Non-interactive preview embedded in the homepage promo card — the whole card is a single
// <Link>, so the map itself ignores pointer events and every click just navigates to /map.
export function PropertyMapPreview({ properties }: { properties: Property[] }) {
  const points = properties
    .filter((p) => p.location)
    .map((p) => ({ id: p.id, lat: p.location!.latitude, lng: p.location!.longitude }));

  return (
    <MapContainer
      center={MOLDOVA_CENTER}
      zoom={7}
      className="h-full w-full"
      style={{ pointerEvents: "none" }}
      zoomControl={false}
      attributionControl={false}
      dragging={false}
      scrollWheelZoom={false}
      doubleClickZoom={false}
      boxZoom={false}
      keyboard={false}
      touchZoom={false}
    >
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
      {points.map((point) => (
        <Marker key={point.id} position={[point.lat, point.lng]} icon={createPropertyMarkerIcon()} />
      ))}
    </MapContainer>
  );
}
