import { describe, expect, it } from "vitest";
import { parseCachedAmenities } from "@/lib/api/amenities";

describe("parseCachedAmenities", () => {
  const current = [{ id: "1", key: "sauna", labelRo: "Saună", category: "Leisure" }];

  it("reuses a cached list of the current shape", () => {
    expect(parseCachedAmenities(JSON.stringify(current))).toEqual(current);
  });

  it("rejects the old shape without a category, so it gets refetched", () => {
    expect(parseCachedAmenities(JSON.stringify([{ id: "1", key: "sauna", labelRo: "Saună" }]))).toBeUndefined();
  });

  it.each([null, "", "not json", "[]", "{}"])("treats %j as a cache miss", (raw) => {
    expect(parseCachedAmenities(raw)).toBeUndefined();
  });
});
