import L from "leaflet";
import type { MarkerCluster } from "leaflet";

// IMOVA's accent color (Tailwind `--color-accent-500`/`--color-accent-700` in globals.css) —
// the same orange used for the primary CTA button, accent badges, and this file's own pin, so
// every marker style on the map reads as one consistent brand color rather than introducing a
// new one for clusters/price pins.
const ACCENT_500 = "#e86a33";
const ACCENT_700 = "#b8481e";

// Same teardrop-pin path used for the location glyph on PropertyCard/PropertyDetail, so map
// markers read as the same visual language as the rest of the site.
const PIN_PATH = "M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z";

function pinSvg(fill: string, size: number) {
  return `<svg width="${size}" height="${size}" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" style="filter:drop-shadow(0 2px 3px rgba(11,22,32,0.35))">
    <path d="${PIN_PATH}" fill="${fill}" stroke="white" stroke-width="1.2" />
    <circle cx="12" cy="10" r="2.6" fill="white" />
  </svg>`;
}

export function createPropertyMarkerIcon(active = false): L.DivIcon {
  const size = active ? 34 : 28;
  return L.divIcon({
    html: pinSvg(active ? ACCENT_700 : ACCENT_500, size),
    className: "",
    iconSize: [size, size],
    iconAnchor: [size / 2, size],
    popupAnchor: [0, -size],
  });
}

// Solid circle cluster icon for the /map explorer (Leaflet.markercluster's `iconCreateFunction`)
// — same shape/layout as a typical cluster marker, restyled in IMOVA's accent color instead of
// the plugin's default red/orange gradient.
export function createClusterIcon(cluster: MarkerCluster): L.DivIcon {
  const count = cluster.getChildCount();
  const size = count >= 100 ? 46 : count >= 10 ? 42 : 38;
  return L.divIcon({
    html: `<div style="
        display: flex; align-items: center; justify-content: center;
        width: ${size}px; height: ${size}px; border-radius: 9999px;
        background: ${ACCENT_500}; color: #fff; font-weight: 600; font-size: 13px;
        box-shadow: 0 2px 6px rgba(11,22,32,0.35);
      ">${count}</div>`,
    className: "",
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
  });
}

// White rounded price pill for a standalone (non-clustered) listing on the /map explorer, in
// place of the plain teardrop pin — e.g. "127 000 €". Width is content-driven (no fixed
// iconSize), self-centered above the marker's lat/lng via a CSS transform, since the price text
// length varies per listing.
export function createPropertyPricePinIcon(label: string, active = false): L.DivIcon {
  const color = active ? ACCENT_700 : ACCENT_500;
  return L.divIcon({
    html: `<div style="
        transform: translate(-50%, -100%);
        white-space: nowrap;
        display: inline-flex; align-items: center;
        padding: 5px 10px;
        border-radius: 9999px;
        background: #fff;
        border: 1.5px solid ${color};
        color: ${color};
        font-weight: 600; font-size: 12px;
        box-shadow: 0 2px 6px rgba(11,22,32,0.25);
      ">${label}</div>`,
    className: "",
    iconSize: [0, 0],
    iconAnchor: [0, 0],
    popupAnchor: [0, -30],
  });
}
