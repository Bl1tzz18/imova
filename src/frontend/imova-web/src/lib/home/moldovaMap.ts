import { MOLDOVA_OUTLINE } from "@/lib/home/moldovaOutline";
import type { Listing } from "@/types/listing";

// The home page's map of Moldova (MoldovaMap): a field of dots inside the border, with one spot per
// raion that has listings. Positions are fractions of the map box (0–1 from the left and from the
// top), so the spots can be laid out with plain percentages and the dots drawn at any size.
// Pure (Vitest-covered).

// Longitude is squeezed by cos(47°), Moldova's middle latitude, so the country keeps its shape.
const LONGITUDE_SCALE = Math.cos((47 * Math.PI) / 180);

const xs = MOLDOVA_OUTLINE.map(([lon]) => lon * LONGITUDE_SCALE);
const ys = MOLDOVA_OUTLINE.map(([, lat]) => lat);
const MIN_X = Math.min(...xs);
const MAX_X = Math.max(...xs);
const MIN_Y = Math.min(...ys);
const MAX_Y = Math.max(...ys);

// Width ÷ height of the country as drawn — the map box takes this aspect ratio.
export const MAP_ASPECT_RATIO = (MAX_X - MIN_X) / (MAX_Y - MIN_Y);

export type MapPoint = { x: number; y: number };

export function project(longitude: number, latitude: number): MapPoint {
  return {
    x: (longitude * LONGITUDE_SCALE - MIN_X) / (MAX_X - MIN_X),
    y: (MAX_Y - latitude) / (MAX_Y - MIN_Y),
  };
}

// Whether a point of the map box (same 0–1 fractions) falls inside the border (ray casting).
export function insideMoldova({ x, y }: MapPoint): boolean {
  const px = MIN_X + x * (MAX_X - MIN_X);
  const py = MAX_Y - y * (MAX_Y - MIN_Y);
  let inside = false;
  for (let i = 0, j = MOLDOVA_OUTLINE.length - 1; i < MOLDOVA_OUTLINE.length; j = i++) {
    const xi = MOLDOVA_OUTLINE[i][0] * LONGITUDE_SCALE;
    const yi = MOLDOVA_OUTLINE[i][1];
    const xj = MOLDOVA_OUTLINE[j][0] * LONGITUDE_SCALE;
    const yj = MOLDOVA_OUTLINE[j][1];
    if (yi > py !== yj > py && px < ((xj - xi) * (py - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
}

export type MapSpot = {
  raionId: string;
  raionName: string;
  count: number;
  // Where the raion's listings are, on average.
  position: MapPoint;
  // 0–1: how big the dot is drawn, by the square root of the count against the busiest raion's.
  weight: number;
  // Whether its name is shown, or only its dot (it would sit on top of a busier raion's label).
  labelled: boolean;
};

// Two labels closer than this (as fractions of the box) would overlap.
const LABEL_CLEARANCE = { x: 0.3, y: 0.07 };

// One spot per raion with listings that have coordinates (Chișinău's sectors fall under Chișinău),
// busiest first. Listings without coordinates aren't counted: they can't be placed.
export function mapSpots(listings: readonly Listing[]): MapSpot[] {
  const byRaion = new Map<string, { raionName: string; count: number; lat: number; lng: number }>();
  for (const listing of listings) {
    const location = listing.property.location;
    if (!location || location.latitude == null || location.longitude == null) continue;
    const entry = byRaion.get(location.raionId) ?? { raionName: location.raionName, count: 0, lat: 0, lng: 0 };
    entry.count += 1;
    entry.lat += location.latitude;
    entry.lng += location.longitude;
    byRaion.set(location.raionId, entry);
  }

  const sorted = [...byRaion.entries()]
    .map(([raionId, e]) => ({ raionId, raionName: e.raionName, count: e.count, position: project(e.lng / e.count, e.lat / e.count) }))
    .sort((a, b) => b.count - a.count || a.raionName.localeCompare(b.raionName));
  const busiest = sorted[0]?.count ?? 0;

  const labelledSoFar: MapPoint[] = [];
  return sorted.map((spot) => {
    const clashes = labelledSoFar.some(
      (p) => Math.abs(p.x - spot.position.x) < LABEL_CLEARANCE.x && Math.abs(p.y - spot.position.y) < LABEL_CLEARANCE.y,
    );
    if (!clashes) labelledSoFar.push(spot.position);
    return { ...spot, weight: Math.sqrt(spot.count / busiest), labelled: !clashes };
  });
}
