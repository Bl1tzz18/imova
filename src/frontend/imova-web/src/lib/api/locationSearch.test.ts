import { describe, expect, it } from "vitest";
import { locationParams, type LocationSuggestion } from "@/lib/api/locationSearch";

const suggestion = (kind: LocationSuggestion["kind"]): LocationSuggestion => ({
  kind,
  id: "own-id",
  name: "Name",
  raionId: "raion-id",
  raionName: "Raion",
});

describe("locationParams", () => {
  it("maps a raion to raionId only", () => {
    expect(locationParams({ ...suggestion("Raion"), raionId: "own-id" })).toEqual({ raionId: ["own-id"] });
  });

  it("maps a localitate to its raion plus localitateId", () => {
    expect(locationParams(suggestion("Localitate"))).toEqual({ raionId: ["raion-id"], localitateId: ["own-id"] });
  });

  it("maps a Chișinău sector to Chișinău plus chisinauSectorId", () => {
    expect(locationParams(suggestion("Sector"))).toEqual({ raionId: ["raion-id"], chisinauSectorId: ["own-id"] });
  });
});
