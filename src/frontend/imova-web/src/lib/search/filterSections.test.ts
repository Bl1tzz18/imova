import { describe, expect, it } from "vitest";
import { filterSections } from "@/lib/search/filterSections";
import type { Amenity, Proximity } from "@/types/listing";

const amenity = (id: string, category: string, types: string[]): Amenity => ({
  id,
  key: id,
  labelRo: id,
  category,
  applicablePropertyTypes: types,
});

const AMENITIES = [
  amenity("furnished", "Comfort", ["Apartment", "House", "Room"]),
  amenity("alarm", "Security", ["Apartment", "House", "Commercial"]),
  amenity("elevator", "General", ["Apartment"]),
  amenity("pool", "Leisure", ["House"]),
];
const PROXIMITIES: Proximity[] = [{ id: "school", key: "school", labelRo: "Școală", applicablePropertyTypes: ["Apartment"] }];

const ids = (state: Record<string, string[]>) => filterSections(state, AMENITIES, PROXIMITIES).map((s) => s.id);
const find = (state: Record<string, string[]>, id: string) => filterSections(state, AMENITIES, PROXIMITIES).find((s) => s.id === id)!;

describe("filterSections", () => {
  it("without a single property type, shows generic area and amenity sections", () => {
    expect(ids({})).toEqual(["basics", "location", "price", "area", "amenities", "proximities"]);
    expect(find({}, "amenities").amenities).toHaveLength(4);
  });

  it("limits the generic amenity list to the selected types", () => {
    const section = find({ propertyType: ["Commercial", "Room"] }, "amenities");
    expect(section.amenities.map((a) => a.id)).toEqual(["furnished", "alarm"]);
  });

  it("groups an apartment's filters like the listing form", () => {
    expect(ids({ propertyType: ["Apartment"] })).toEqual([
      "basics", "location", "price", "structure", "areas", "systems", "comfort", "security", "other", "proximities",
    ]);
    const structure = find({ propertyType: ["Apartment"] }, "structure");
    expect(structure.typeFilters.map((f) => f.field)).toEqual(["housingStockType", "layout", "rooms", "floor", "bathrooms"]);
    expect(structure.openByDefault).toBe(true);
    expect(find({ propertyType: ["Apartment"] }, "areas")).toMatchObject({ area: true, typeFilters: [], openByDefault: false });
    expect(find({ propertyType: ["Apartment"] }, "other").amenities.map((a) => a.id)).toEqual(["elevator"]);
  });

  it("puts the area range with the type's own area filters", () => {
    const areas = find({ propertyType: ["House"] }, "areas");
    expect(areas.area).toBe(true);
    expect(areas.typeFilters.map((f) => f.field)).toEqual(["landAreaM2"]);
    expect(ids({ propertyType: ["Land"] })).toEqual(["basics", "location", "price", "typeArea", "utilitiesAccess", "proximities"]);
  });

  it("adds rental terms only for a rental search", () => {
    expect(ids({ transactionType: ["Rent"] })).toContain("rentalTerms");
    expect(ids({ transactionType: ["Sale"] })).not.toContain("rentalTerms");
  });

  it("counts the active filters of each section, a range once", () => {
    const state = {
      transactionType: ["Rent"],
      propertyType: ["Apartment"],
      minPriceEur: ["100"],
      maxPriceEur: ["500"],
      minRooms: ["2"],
      maxRooms: ["3"],
      layout: ["Separate"],
      petsAllowed: ["true"],
      amenityIds: ["furnished", "elevator"],
    };
    const counts = Object.fromEntries(filterSections(state, AMENITIES, PROXIMITIES).map((s) => [s.id, s.activeCount]));
    expect(counts).toMatchObject({ basics: 2, location: 0, price: 1, structure: 2, areas: 0, rentalTerms: 1, comfort: 1, security: 0, other: 1, proximities: 0 });
  });

  it("drops amenity and proximity sections until their lists have loaded", () => {
    expect(filterSections({}, [], []).map((s) => s.id)).toEqual(["basics", "location", "price", "area"]);
  });
});
