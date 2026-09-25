import { describe, expect, it } from "vitest";
import { parseCachedProximities } from "@/lib/api/proximities";

describe("parseCachedProximities", () => {
  const current = [{ id: "1", key: "school", labelRo: "Școală", applicablePropertyTypes: ["House", "Land"] }];

  it("reuses a cached list of the current shape", () => {
    expect(parseCachedProximities(JSON.stringify(current))).toEqual(current);
  });

  it("rejects items without applicable property types, so they get refetched", () => {
    expect(parseCachedProximities(JSON.stringify([{ id: "1", key: "school", labelRo: "Școală" }]))).toBeUndefined();
  });

  it.each([null, "", "not json", "[]", "{}"])("treats %j as a cache miss", (raw) => {
    expect(parseCachedProximities(raw)).toBeUndefined();
  });
});
