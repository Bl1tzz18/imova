import { describe, expect, it } from "vitest";
import { filterSections, type FilterItem } from "@/lib/search/filterSections";
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

type State = Record<string, string[]>;
const sections = (state: State) => filterSections(state, AMENITIES, PROXIMITIES);
const ids = (state: State) => sections(state).map((s) => s.id);
const find = (state: State, id: string) => sections(state).find((s) => s.id === id)!;
const itemNames = (items: readonly FilterItem[]) => items.map((i) => (i.kind === "attribute" ? i.filter.field : i.kind));

describe("filterSections", () => {
  it("without a property type, shows only what every listing has", () => {
    expect(ids({})).toEqual(["basics", "location", "price", "area", "amenities", "proximities"]);
    expect(find({}, "amenities").amenities).toHaveLength(4);
  });

  it("with a type, follows its listing form's sections and fields", () => {
    const apartment = { propertyType: ["Apartment"] };
    expect(ids(apartment)).toEqual([
      "basics", "location", "price", "structure", "areas", "systems", "finishing", "comfort", "security", "other", "proximities",
    ]);
    expect(itemNames(find(apartment, "structure").items)).toEqual([
      "yearBuilt", "housingStockType", "buildingMaterial", "finishCondition", "layout", "rooms", "floor", "totalFloors", "bathrooms",
    ]);
    expect(itemNames(find(apartment, "areas").items)).toEqual(["area", "livingAreaM2", "kitchenAreaM2"]);
    expect(itemNames(find(apartment, "finishing").items)).toEqual(["floorMaterial"]);
    expect(find(apartment, "structure").openByDefault).toBe(true);
    expect(find(apartment, "areas").openByDefault).toBe(false);
    expect(find(apartment, "other").amenities.map((a) => a.id)).toEqual(["elevator"]);
  });

  it("shows a conditional field only once its controlling filter unlocks it", () => {
    const heating = (state: State) => itemNames(find(state, "systems").items);
    expect(heating({ propertyType: ["House"] })).toEqual(["heatingSystem", "waterSupply", "sewerage", "gasSupply"]);
    expect(heating({ propertyType: ["House"], heatingSystem: ["OwnBoiler"] })).toEqual([
      "heatingSystem", "heatingEnergySource", "heatingDistribution", "waterSupply", "sewerage", "gasSupply",
    ]);
  });

  it("covers the small layouts too, with year built and the general condition where the form asks them", () => {
    expect(ids({ propertyType: ["Garage"] })).toEqual(["basics", "location", "price", "typeArea", "proximities"]);
    expect(itemNames(find({ propertyType: ["Garage"] }, "typeArea").items)).toEqual(["area", "yearBuilt", "condition", "parkingType"]);
    expect(ids({ propertyType: ["Land"] })).toEqual(["basics", "location", "price", "typeArea", "utilitiesAccess", "proximities"]);
    expect(itemNames(find({ propertyType: ["Land"] }, "typeArea").items)).toEqual(["area", "plotType", "locationContext"]);
  });

  it("adds rental terms last, only for a rental search", () => {
    expect(ids({ transactionType: ["Rent"], propertyType: ["Room"] }).at(-1)).toBe("rentalTerms");
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
      layout: ["Studio"],
      minAreaM2: ["40"],
      petsAllowed: ["true"],
      amenityIds: ["furnished", "elevator"],
    };
    const counts = Object.fromEntries(sections(state).map((s) => [s.id, s.activeCount]));
    expect(counts).toMatchObject({
      basics: 2, location: 0, price: 1, structure: 2, areas: 1, systems: 0, rentalTerms: 1, comfort: 1, security: 0, other: 1, proximities: 0,
    });
  });

  it("drops amenity and proximity sections until their lists have loaded", () => {
    expect(filterSections({}, [], []).map((s) => s.id)).toEqual(["basics", "location", "price", "area"]);
  });
});
