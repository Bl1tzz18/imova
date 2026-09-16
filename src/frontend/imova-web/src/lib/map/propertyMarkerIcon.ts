import L from "leaflet";

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
    html: pinSvg(active ? "#b8481e" : "#e86a33", size),
    className: "",
    iconSize: [size, size],
    iconAnchor: [size / 2, size],
    popupAnchor: [0, -size],
  });
}
