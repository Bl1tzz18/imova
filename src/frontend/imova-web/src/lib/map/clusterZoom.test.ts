import { describe, expect, it } from "vitest";
import { clusterClickZoom } from "./clusterZoom";

describe("clusterClickZoom", () => {
  it("zooms in as far as still shows every listing in the cluster", () => {
    // Two pins ~1 km apart seen from zoom 13: zoom 15 fits both — not the old fixed jump to 18,
    // which left them both off screen.
    expect(clusterClickZoom(13, 15, 19)).toBe(15);
  });

  it("always goes at least one level further in", () => {
    expect(clusterClickZoom(13, 13, 19)).toBe(14);
    expect(clusterClickZoom(13, 11, 19)).toBe(14);
  });

  it("stops at the map's maximum zoom", () => {
    expect(clusterClickZoom(17, 22, 19)).toBe(19);
    expect(clusterClickZoom(18, 18, 19)).toBe(19);
  });
});
