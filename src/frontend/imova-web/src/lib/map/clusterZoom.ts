// How far a click on a map cluster zooms in. The cluster's listings must all stay on screen: the
// map is centred on their bounds and zoomed to `fitZoom` — the closest zoom that still shows all of
// them (Leaflet's getBoundsZoom, with padding). Always at least one level further than now, so every
// click makes progress (a cluster only exists when its pins are within a few dozen pixels of each
// other, so one more level can't push them off screen), and never past the map's maximum.
// Pure (Vitest-covered).
export function clusterClickZoom(currentZoom: number, fitZoom: number, maxZoom: number): number {
  return Math.min(Math.max(fitZoom, currentZoom + 1), maxZoom);
}
