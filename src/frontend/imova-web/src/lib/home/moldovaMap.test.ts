import { describe, expect, it } from "vitest";
import type { Listing } from "@/types/listing";
import { insideMoldova, MAP_ASPECT_RATIO, mapSpots, project } from "./moldovaMap";

const listing = (raionId: string, raionName: string, latitude: number | null, longitude: number | null) =>
  ({ property: { location: { raionId, raionName, latitude, longitude } } }) as unknown as Listing;

const CHISINAU = { lat: 47.0105, lng: 28.8638 };

describe("project", () => {
  it("places Moldova's towns where they are on a map", () => {
    const chisinau = project(CHISINAU.lng, CHISINAU.lat);
    const balti = project(27.93, 47.76);
    const cahul = project(28.19, 45.9);
    // Bălți is north-west of Chișinău, Cahul far south (only the Giurgiulești tip is further).
    expect(balti.x).toBeLessThan(chisinau.x);
    expect(balti.y).toBeLessThan(chisinau.y);
    expect(cahul.y).toBeGreaterThan(0.8);
    for (const p of [chisinau, balti, cahul]) {
      expect(p.x).toBeGreaterThanOrEqual(0);
      expect(p.x).toBeLessThanOrEqual(1);
    }
  });

  it("keeps the country taller than wide", () => {
    expect(MAP_ASPECT_RATIO).toBeGreaterThan(0.6);
    expect(MAP_ASPECT_RATIO).toBeLessThan(0.9);
  });
});

describe("insideMoldova", () => {
  it("knows the towns are inside and the neighbours aren't", () => {
    expect(insideMoldova(project(CHISINAU.lng, CHISINAU.lat))).toBe(true);
    expect(insideMoldova(project(27.93, 47.76))).toBe(true);
    expect(insideMoldova(project(27.59, 47.16))).toBe(false); // Iași, Romania
    expect(insideMoldova(project(30.72, 46.48))).toBe(false); // Odesa, Ukraine
  });
});

describe("mapSpots", () => {
  it("puts one spot per raion where its listings are, busiest first", () => {
    const spots = mapSpots([
      listing("bl", "Bălți", 47.76, 27.93),
      listing("ch", "Chișinău", 47.0, 28.8),
      listing("ch", "Chișinău", 47.02, 28.9),
      listing("ch", "Chișinău", null, null),
    ]);

    expect(spots.map((s) => [s.raionName, s.count])).toEqual([
      ["Chișinău", 2],
      ["Bălți", 1],
    ]);
    const middle = project(28.85, 47.01);
    expect(spots[0].position.x).toBeCloseTo(middle.x, 10);
    expect(spots[0].position.y).toBeCloseTo(middle.y, 10);
    expect(spots[0].weight).toBe(1);
    expect(spots[1].weight).toBeCloseTo(Math.SQRT1_2);
  });

  it("only labels a spot whose name wouldn't sit on a busier one's", () => {
    const spots = mapSpots([
      listing("ch", "Chișinău", 47.01, 28.86),
      listing("ch", "Chișinău", 47.01, 28.86),
      listing("ia", "Ialoveni", 46.94, 28.78),
      listing("ca", "Cahul", 45.9, 28.19),
    ]);

    expect(Object.fromEntries(spots.map((s) => [s.raionName, s.labelled]))).toEqual({
      Chișinău: true,
      Ialoveni: false,
      Cahul: true,
    });
  });

  it("is empty without listings that can be placed", () => {
    expect(mapSpots([])).toEqual([]);
    expect(mapSpots([listing("ch", "Chișinău", null, null)])).toEqual([]);
  });
});
