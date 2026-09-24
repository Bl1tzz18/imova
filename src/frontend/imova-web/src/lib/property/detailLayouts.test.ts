import { describe, expect, it } from "vitest";
import { ATTRIBUTE_SCHEMA, attributeInputName, readAttributes } from "@/lib/property/attributeSchema";
import {
  DETAIL_LAYOUTS,
  amenitiesForSection,
  detailLayoutFor,
  isSectionComplete,
  sectionsFor,
  selectableAmenities,
  type DetailLayout,
  type DetailSection,
} from "@/lib/property/detailLayouts";
import type { Amenity } from "@/types/listing";

const amenity = (key: string, category: string, types: string[]): Amenity => ({
  id: key,
  key,
  labelRo: key,
  category,
  applicablePropertyTypes: types,
});

const AMENITIES = [
  amenity("furnished", "Comfort", ["Apartment", "House", "Commercial", "Room"]),
  amenity("fireplace", "Comfort", ["House"]),
  amenity("dishwasher", "Comfort", ["Apartment", "House", "Room"]),
  amenity("alarm_system", "Security", ["Apartment", "House", "Commercial", "Garage"]),
  amenity("sauna", "Leisure", ["House"]),
  amenity("parking", "Leisure", ["Apartment", "House", "Commercial"]),
  amenity("balcony", "General", ["Apartment", "House", "Room"]),
  amenity("elevator", "General", ["Apartment", "Commercial"]),
  amenity("guarded", "Security", ["Land", "Garage"]),
  amenity("near_forest", "Leisure", ["Land", "House"]),
  amenity("electricity", "General", ["Garage"]),
  amenity("kitchen_access", "Comfort", ["Room"]),
];

const layout = (type: string): DetailLayout => detailLayoutFor(type)!;
const section = (type: string, id: string): DetailSection => layout(type).sections.find((s) => s.id === id)!;
const keysIn = (type: string, id: string) =>
  amenitiesForSection(layout(type), section(type, id), AMENITIES, type).map((a) => a.key);

// A getter over attribute values keyed by their bare field name, plus core fields by input name.
function getter(values: Record<string, string>) {
  return (name: string) => values[name.replace(/^attr\./, "")] ?? null;
}

function formWith(values: Record<string, string>) {
  const form = new FormData();
  for (const [name, value] of Object.entries(values)) form.set(attributeInputName(name), value);
  return form;
}

describe("layouts", () => {
  it("exist for the detailed types and not for Garage/Room (flat form)", () => {
    expect(Object.keys(DETAIL_LAYOUTS).sort()).toEqual(["Apartment", "Commercial", "House", "Land"]);
    expect(detailLayoutFor("Garage")).toBeUndefined();
    expect(detailLayoutFor("Room")).toBeUndefined();
  });

  it.each(["Apartment", "House", "Land", "Commercial"])("place every %s attribute in exactly one section", (type) => {
    const placed = layout(type).sections.flatMap((s) => s.attributeFields);
    const schema = ATTRIBUTE_SCHEMA[type as keyof typeof ATTRIBUTE_SCHEMA].map((f) => f.name);
    expect([...placed].sort()).toEqual([...schema].sort());
    expect(new Set(placed).size).toBe(placed.length);
  });

  it.each(["Apartment", "House", "Land", "Commercial"])("put the %s total area in exactly one section", (type) => {
    expect(layout(type).sections.filter((s) => s.coreFields.includes("totalAreaM2"))).toHaveLength(1);
  });

  it("use the requested section order, with the rental-only rules section last", () => {
    expect(layout("House").sections.map((s) => s.id)).toEqual([
      "structure", "areas", "systems", "finishing", "comfort", "security", "leisure", "rentalRules",
    ]);
    expect(layout("Apartment").sections.map((s) => s.id)).toEqual([
      "structure", "areas", "systems", "finishing", "comfort", "security", "other", "rentalRules",
    ]);
    expect(layout("Land").sections.map((s) => s.id)).toEqual(["typeArea", "utilitiesAccess", "surroundings"]);
    expect(layout("Commercial").sections.map((s) => s.id)).toEqual(["structure", "areas", "systems", "amenities"]);
  });

  it("have exactly one catch-all amenity section per layout", () => {
    for (const l of Object.values(DETAIL_LAYOUTS)) {
      expect(l!.sections.filter((s) => s.catchAllAmenities)).toHaveLength(1);
    }
  });
});

describe("amenities", () => {
  it("only offer amenities that apply to the property type", () => {
    const garage = selectableAmenities(AMENITIES, "Garage").map((a) => a.key);
    expect(garage.sort()).toEqual(["alarm_system", "electricity", "guarded"]);
    expect(garage).not.toContain("sauna");
  });

  it.each(["Apartment", "House", "Room", "Commercial"])("offer Furnished for a %s (sale and rent alike)", (type) => {
    expect(selectableAmenities(AMENITIES, type).map((a) => a.key)).toContain("furnished");
  });

  it.each(["Apartment", "House"])("put Furnished in the %s comfort section", (type) => {
    expect(keysIn(type, "comfort")).toContain("furnished");
  });

  it("group House amenities by category, with General ones in the catch-all leisure section", () => {
    expect(keysIn("House", "comfort").sort()).toEqual(["dishwasher", "fireplace", "furnished"]);
    expect(keysIn("House", "security")).toEqual(["alarm_system"]);
    expect(keysIn("House", "leisure").sort()).toEqual(["balcony", "near_forest", "parking", "sauna"]);
  });

  it("put Apartment elevator/balcony and leisure-type amenities in 'other'", () => {
    expect(keysIn("Apartment", "comfort").sort()).toEqual(["dishwasher", "furnished"]);
    expect(keysIn("Apartment", "other").sort()).toEqual(["balcony", "elevator", "parking"]);
  });

  it("show every applicable amenity in single-section layouts", () => {
    expect(keysIn("Land", "surroundings").sort()).toEqual(["guarded", "near_forest"]);
    expect(keysIn("Commercial", "amenities").sort()).toEqual(["alarm_system", "elevator", "furnished", "parking"]);
  });
});

describe("section completion", () => {
  it("counts a field section complete only when its required fields are filled", () => {
    const filled = { houseType: "Villa", buildingMaterial: "Brick", finishCondition: "NoRepair", houseFloors: "2", rooms: "5" };
    expect(isSectionComplete(section("House", "structure"), "House", getter(filled), false)).toBe(true);
    expect(isSectionComplete(section("House", "structure"), "House", getter({ ...filled, rooms: "" }), true)).toBe(false);
  });

  it("requires the total area (a core field) where it's placed", () => {
    const typeArea = section("Land", "typeArea");
    const plot = { plotType: "Forest", locationContext: "WithinTownLimits" };
    expect(isSectionComplete(typeArea, "Land", getter(plot), false)).toBe(false);
    expect(isSectionComplete(typeArea, "Land", getter({ ...plot, totalAreaM2: "1200" }), false)).toBe(true);
  });

  it("counts a section with nothing required complete once opened", () => {
    expect(isSectionComplete(section("Apartment", "finishing"), "Apartment", getter({}), false)).toBe(false);
    expect(isSectionComplete(section("Apartment", "other"), "Apartment", getter({}), false)).toBe(false);
    expect(isSectionComplete(section("Apartment", "other"), "Apartment", getter({}), true)).toBe(true);
  });

  it("counts the Land utilities section complete once opened — every answer there is optional", () => {
    const utilities = section("Land", "utilitiesAccess");
    expect(isSectionComplete(utilities, "Land", getter({}), false)).toBe(false);
    expect(isSectionComplete(utilities, "Land", getter({}), true)).toBe(true);
  });

  it("don't wait on the optional House/Apartment materials and systems", () => {
    const houseStructure = { houseType: "Villa", houseFloors: "2", rooms: "5" };
    expect(isSectionComplete(section("House", "structure"), "House", getter(houseStructure), false)).toBe(true);
    expect(isSectionComplete(section("House", "areas"), "House", getter({ totalAreaM2: "180", landAreaM2: "600" }), false)).toBe(
      true,
    );
    expect(isSectionComplete(section("House", "systems"), "House", getter({}), false)).toBe(true);
    const apartmentStructure = { rooms: "2", floor: "3", totalFloors: "9" };
    expect(isSectionComplete(section("Apartment", "structure"), "Apartment", getter(apartmentStructure), false)).toBe(true);
    expect(isSectionComplete(section("Apartment", "systems"), "Apartment", getter({}), false)).toBe(true);
  });
});

describe("conditional fields", () => {
  it.each(["House", "Apartment"])("%s heating details are required only for boiler/heat pump/solar", (type) => {
    const systems = section(type, "systems");
    const base = { waterSupply: "Well", sewerage: "SepticTank", gasSupply: "false" };
    expect(isSectionComplete(systems, type, getter({ ...base, heatingSystem: "OwnBoiler" }), false)).toBe(false);
    expect(
      isSectionComplete(
        systems,
        type,
        getter({ ...base, heatingSystem: "HeatPump", heatingEnergySource: "Electricity", heatingDistribution: "Air" }),
        false,
      ),
    ).toBe(true);
    expect(isSectionComplete(systems, type, getter({ ...base, heatingSystem: "DistrictHeating" }), false)).toBe(true);
  });

  it("drop heating details from the payload when the heating system doesn't use them", () => {
    const attributes = readAttributes(
      "Apartment",
      formWith({ heatingSystem: "DistrictHeating", heatingEnergySource: "Gas", heatingDistribution: "Radiators" }),
    );
    expect(attributes.heatingSystem).toBe("DistrictHeating");
    expect(attributes).not.toHaveProperty("heatingEnergySource");
    expect(attributes).not.toHaveProperty("heatingDistribution");
  });

  it("send the soil quality score only for agricultural land", () => {
    expect(readAttributes("Land", formWith({ plotType: "Agricultural", soilQualityScore: "64" })).soilQualityScore).toBe(64);
    expect(readAttributes("Land", formWith({ plotType: "Forest", soilQualityScore: "64" }))).not.toHaveProperty("soilQualityScore");
  });

  it("send the number of offices only for office space", () => {
    expect(readAttributes("Commercial", formWith({ spaceType: "OfficeSpace", numberOfOffices: "6" })).numberOfOffices).toBe(6);
    expect(readAttributes("Commercial", formWith({ spaceType: "Warehouse", numberOfOffices: "6" }))).not.toHaveProperty(
      "numberOfOffices",
    );
  });

  it("keep negative commercial floors (basement levels)", () => {
    expect(readAttributes("Commercial", formWith({ floor: "-1" })).floor).toBe(-1);
  });
});

describe("yes/no answers", () => {
  it.each([
    ["true", true],
    ["false", false],
  ])("submit %s as a boolean", (raw, expected) => {
    expect(readAttributes("Land", formWith({ irrigationSystem: raw })).irrigationSystem).toBe(expected);
  });

  it("are omitted when not answered", () => {
    expect(readAttributes("House", formWith({ gasSupply: "" }))).not.toHaveProperty("gasSupply");
  });
});

describe("finish condition options", () => {
  it("list the most common conditions first, identically for every type that uses them", () => {
    for (const type of ["Apartment", "House", "Commercial"] as const) {
      const field = ATTRIBUTE_SCHEMA[type].find((f) => f.name === "finishCondition");
      expect(field && "options" in field ? field.options.slice(0, 3) : []).toEqual([
        "EuroRenovated", "CosmeticRepair", "WhiteStructure",
      ]);
    }
  });
});

describe("rental rules section (pets)", () => {
  it.each(["House", "Apartment"])("only exists on a %s rental", (type) => {
    expect(sectionsFor(layout(type), "Rent").map((s) => s.id)).toContain("rentalRules");
    expect(sectionsFor(layout(type), "Sale").map((s) => s.id)).not.toContain("rentalRules");
    expect(sectionsFor(layout(type), "Sale")).toHaveLength(7);
    expect(sectionsFor(layout(type), "Rent")).toHaveLength(8);
  });

  it.each(["Land", "Commercial"])("is never shown for %s (pets don't apply)", (type) => {
    expect(sectionsFor(layout(type), "Rent").map((s) => s.id)).not.toContain("rentalRules");
  });

  it("counts as complete only once pets is answered Yes or No", () => {
    const rules = section("Apartment", "rentalRules");
    expect(isSectionComplete(rules, "Apartment", getter({}), true)).toBe(false);
    expect(isSectionComplete(rules, "Apartment", (name) => (name === "rental.petsAllowed" ? "false" : null), false)).toBe(true);
  });
});
