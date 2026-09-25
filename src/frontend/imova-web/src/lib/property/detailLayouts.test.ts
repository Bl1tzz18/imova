import { describe, expect, it } from "vitest";
import {
  ATTRIBUTE_SCHEMA,
  attributeInputName,
  hasBuilding,
  readAttributes,
  usesGeneralCondition,
} from "@/lib/property/attributeSchema";
import {
  DETAIL_LAYOUTS,
  amenitiesForSection,
  detailLayoutFor,
  isAmenitySection,
  isProximitySection,
  isSectionComplete,
  sectionsFor,
  selectableAmenities,
  selectableProximities,
  type DetailLayout,
  type DetailSection,
} from "@/lib/property/detailLayouts";
import type { Amenity, Proximity } from "@/types/listing";

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
  it("exist for every property type — Garage and Room included, no flat form", () => {
    expect(Object.keys(DETAIL_LAYOUTS).sort()).toEqual(["Apartment", "Commercial", "Garage", "House", "Land", "Room"]);
  });

  it.each(["Apartment", "House", "Land", "Commercial", "Garage", "Room"])("place every %s attribute in exactly one section", (type) => {
    const placed = layout(type).sections.flatMap((s) => s.attributeFields);
    const schema = ATTRIBUTE_SCHEMA[type as keyof typeof ATTRIBUTE_SCHEMA].map((f) => f.name);
    expect([...placed].sort()).toEqual([...schema].sort());
    expect(new Set(placed).size).toBe(placed.length);
  });

  it.each(["Apartment", "House", "Land", "Commercial", "Garage", "Room"])("put the %s total area in exactly one section", (type) => {
    expect(layout(type).sections.filter((s) => s.coreFields.includes("totalAreaM2"))).toHaveLength(1);
  });

  it.each(["Apartment", "House", "Land", "Commercial", "Garage", "Room"])("ask year built and the general condition only where they apply (%s)", (type) => {
    const core = layout(type).sections.flatMap((s) => s.coreFields);
    expect(core.filter((f) => f === "yearBuilt")).toHaveLength(hasBuilding(type) ? 1 : 0);
    expect(core.filter((f) => f === "condition")).toHaveLength(usesGeneralCondition(type) ? 1 : 0);
  });

  it("use the requested section order: proximities right after the amenities, rental-only rules last", () => {
    expect(layout("House").sections.map((s) => s.id)).toEqual([
      "structure", "areas", "systems", "finishing", "comfort", "security", "leisure", "proximities", "rentalRules",
    ]);
    expect(layout("Apartment").sections.map((s) => s.id)).toEqual([
      "structure", "areas", "systems", "finishing", "comfort", "security", "other", "proximities", "rentalRules",
    ]);
    expect(layout("Land").sections.map((s) => s.id)).toEqual(["typeArea", "utilitiesAccess", "proximities"]);
    expect(layout("Commercial").sections.map((s) => s.id)).toEqual(["structure", "areas", "systems", "amenities", "proximities"]);
    expect(layout("Garage").sections.map((s) => s.id)).toEqual(["typeArea", "amenities", "proximities"]);
    expect(layout("Room").sections.map((s) => s.id)).toEqual(["typeArea", "comfort", "other", "proximities", "rentalRules"]);
  });

  it.each(["Apartment", "House", "Commercial", "Garage", "Room"])("give %s exactly one catch-all amenity section", (type) => {
    expect(layout(type).sections.filter((s) => s.catchAllAmenities)).toHaveLength(1);
  });

  it("give Land no amenity section at all (no amenity applies to it)", () => {
    expect(layout("Land").sections.filter(isAmenitySection)).toEqual([]);
  });
});

describe("amenities", () => {
  it("only offer amenities that apply to the property type", () => {
    const garage = selectableAmenities(AMENITIES, "Garage").map((a) => a.key);
    expect(garage.sort()).toEqual(["alarm_system", "electricity"]);
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
    expect(keysIn("House", "leisure").sort()).toEqual(["balcony", "parking", "sauna"]);
  });

  it("put Apartment elevator/balcony and leisure-type amenities in 'other'", () => {
    expect(keysIn("Apartment", "comfort").sort()).toEqual(["dishwasher", "furnished"]);
    expect(keysIn("Apartment", "other").sort()).toEqual(["balcony", "elevator", "parking"]);
  });

  it("split Room amenities into comfort and a catch-all 'other'", () => {
    expect(keysIn("Room", "comfort").sort()).toEqual(["dishwasher", "furnished", "kitchen_access"]);
    expect(keysIn("Room", "other")).toEqual(["balcony"]);
  });

  it("show every applicable amenity in single-section layouts", () => {
    expect(keysIn("Commercial", "amenities").sort()).toEqual(["alarm_system", "elevator", "furnished", "parking"]);
    expect(keysIn("Garage", "amenities").sort()).toEqual(["alarm_system", "electricity"]);
  });
});

describe("proximities (Vecinătăți)", () => {
  const PROXIMITIES: Proximity[] = ["school", "park"].map((key) => ({
    id: key,
    key,
    labelRo: key,
    applicablePropertyTypes: ["Apartment", "House", "Land", "Commercial", "Garage", "Room"],
  }));

  it.each(["Apartment", "House", "Land", "Commercial"])("get exactly one section in the %s layout, apart from amenities", (type) => {
    const sections = layout(type).sections.filter(isProximitySection);
    expect(sections).toHaveLength(1);
    expect(isAmenitySection(sections[0])).toBe(false);
    // The catch-all amenity section doesn't pick them up either — they're a separate list.
    expect(keysIn(type, sections[0].id)).toEqual([]);
  });

  it.each(["Apartment", "House", "Land", "Commercial", "Garage", "Room"])("are all offered for a %s", (type) => {
    expect(selectableProximities(PROXIMITIES, type).map((p) => p.key)).toEqual(["school", "park"]);
  });

  it("only offer proximities that apply to the property type", () => {
    const houseOnly = { ...PROXIMITIES[0], key: "ski_lift", applicablePropertyTypes: ["House"] };
    expect(selectableProximities([houseOnly], "Garage")).toEqual([]);
  });

  it("are optional: the section counts as complete once opened, with nothing selected", () => {
    const proximities = section("House", "proximities");
    expect(isSectionComplete(proximities, "House", getter({}), false)).toBe(false);
    expect(isSectionComplete(proximities, "House", getter({}), true)).toBe(true);
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
  it.each(["House", "Apartment"])("%s heating details are required only for boiler/heat pump", (type) => {
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

  it.each(["House", "Apartment"])("%s solar heating asks for distribution but no energy source", (type) => {
    const systems = section(type, "systems");
    const base = { waterSupply: "Well", sewerage: "SepticTank", gasSupply: "false", heatingSystem: "SolarPanels" };
    expect(isSectionComplete(systems, type, getter(base), false)).toBe(false);
    expect(isSectionComplete(systems, type, getter({ ...base, heatingDistribution: "UnderfloorHeating" }), false)).toBe(true);
  });

  it("drop the energy source but keep the distribution for solar heating", () => {
    const attributes = readAttributes(
      "House",
      formWith({ heatingSystem: "SolarPanels", heatingEnergySource: "Electricity", heatingDistribution: "UnderfloorHeating" }),
    );
    expect(attributes).not.toHaveProperty("heatingEnergySource");
    expect(attributes.heatingDistribution).toBe("UnderfloorHeating");
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
    expect(sectionsFor(layout(type), "Sale")).toHaveLength(8);
    expect(sectionsFor(layout(type), "Rent")).toHaveLength(9);
  });

  it("only exists on a Room rental", () => {
    expect(sectionsFor(layout("Room"), "Sale").map((s) => s.id)).toEqual(["typeArea", "comfort", "other", "proximities"]);
    expect(sectionsFor(layout("Room"), "Rent").map((s) => s.id)).toContain("rentalRules");
  });

  it.each(["Land", "Commercial", "Garage"])("is never shown for %s (pets don't apply)", (type) => {
    expect(sectionsFor(layout(type), "Rent").map((s) => s.id)).not.toContain("rentalRules");
  });

  it("counts as complete only once pets is answered Yes or No", () => {
    const rules = section("Apartment", "rentalRules");
    expect(isSectionComplete(rules, "Apartment", getter({}), true)).toBe(false);
    expect(isSectionComplete(rules, "Apartment", (name) => (name === "rental.petsAllowed" ? "false" : null), false)).toBe(true);
  });
});
