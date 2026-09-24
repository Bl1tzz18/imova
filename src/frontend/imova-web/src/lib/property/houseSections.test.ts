import { describe, expect, it } from "vitest";
import { ATTRIBUTE_SCHEMA, attributeInputName, readAttributes } from "@/lib/property/attributeSchema";
import {
  HOUSE_SECTIONS,
  amenitiesInCategory,
  isSectionComplete,
  selectableAmenities,
  type HouseSection,
} from "@/lib/property/houseSections";
import type { Amenity } from "@/types/listing";

const amenity = (key: string, category: string): Amenity => ({ id: key, key, labelRo: key, category });
const AMENITIES = [
  amenity("furnished", "Comfort"),
  amenity("fireplace", "Comfort"),
  amenity("alarm_system", "Security"),
  amenity("sauna", "Leisure"),
  amenity("elevator", "General"),
];

const section = (id: string): HouseSection => HOUSE_SECTIONS.find((s) => s.id === id)!;

// A getter over attribute values keyed by their bare field name, plus core fields by input name.
function getter(values: Record<string, string>) {
  return (name: string) => values[name.replace(/^attr\./, "")] ?? null;
}

describe("furnished amenity", () => {
  it("is hidden for a rental", () => {
    expect(selectableAmenities(AMENITIES, "Rent").map((a) => a.key)).not.toContain("furnished");
    expect(amenitiesInCategory(AMENITIES, "Comfort", "Rent").map((a) => a.key)).toEqual(["fireplace"]);
  });

  it("stays available for a sale", () => {
    expect(amenitiesInCategory(AMENITIES, "Comfort", "Sale").map((a) => a.key)).toEqual(["furnished", "fireplace"]);
  });

  it("only affects the furnished amenity", () => {
    expect(selectableAmenities(AMENITIES, "Rent")).toHaveLength(AMENITIES.length - 1);
  });
});

describe("house sections", () => {
  it("places every House attribute in exactly one section", () => {
    const placed = HOUSE_SECTIONS.flatMap((s) => s.attributeFields);
    expect([...placed].sort()).toEqual(ATTRIBUTE_SCHEMA.House.map((f) => f.name).sort());
    expect(new Set(placed).size).toBe(placed.length);
  });

  it("orders the seven sections as specified", () => {
    expect(HOUSE_SECTIONS.map((s) => s.id)).toEqual([
      "structure", "areas", "systems", "finishing", "comfort", "security", "leisure",
    ]);
  });

  it("counts a field section complete only when all its required fields are filled", () => {
    const structure = section("structure");
    const filled = { houseType: "Villa", buildingMaterial: "Brick", houseCondition: "NoRepair", houseFloors: "2", rooms: "5" };

    expect(isSectionComplete(structure, getter(filled), false)).toBe(true);
    expect(isSectionComplete(structure, getter({ ...filled, rooms: "" }), true)).toBe(false);
  });

  it("requires the total area (a core field) in the areas section", () => {
    const areas = section("areas");
    const attrs = { livingAreaM2: "120", landAreaM2: "500" };

    expect(isSectionComplete(areas, getter(attrs), false)).toBe(false);
    expect(isSectionComplete(areas, getter({ ...attrs, totalAreaM2: "150" }), false)).toBe(true);
  });

  it("counts an amenity section complete once it has been opened", () => {
    expect(isSectionComplete(section("comfort"), getter({}), false)).toBe(false);
    expect(isSectionComplete(section("comfort"), getter({}), true)).toBe(true);
  });
});

describe("conditional heating details", () => {
  const systems = section("systems");
  const base = { waterSupply: "Well", sewerage: "SepticTank", gasSupply: "false" };

  it.each(["OwnBoiler", "HeatPump", "SolarPanels"])("are required with %s heating", (heatingSystem) => {
    expect(isSectionComplete(systems, getter({ ...base, heatingSystem }), false)).toBe(false);
    expect(
      isSectionComplete(
        systems,
        getter({ ...base, heatingSystem, heatingEnergySource: "Gas", heatingDistribution: "Radiators" }),
        false,
      ),
    ).toBe(true);
  });

  it.each(["DistrictHeating", "Convector", "InfraredPanels", "Stove", "None"])(
    "are not required with %s heating",
    (heatingSystem) => {
      expect(isSectionComplete(systems, getter({ ...base, heatingSystem }), false)).toBe(true);
    },
  );

  it("are dropped from the payload when the heating system doesn't use them", () => {
    const form = new FormData();
    form.set(attributeInputName("heatingSystem"), "DistrictHeating");
    form.set(attributeInputName("heatingEnergySource"), "Gas");
    form.set(attributeInputName("heatingDistribution"), "Radiators");

    const attributes = readAttributes("House", form);

    expect(attributes.heatingSystem).toBe("DistrictHeating");
    expect(attributes).not.toHaveProperty("heatingEnergySource");
    expect(attributes).not.toHaveProperty("heatingDistribution");
  });

  it("are kept in the payload for a boiler", () => {
    const form = new FormData();
    form.set(attributeInputName("heatingSystem"), "OwnBoiler");
    form.set(attributeInputName("heatingEnergySource"), "Gas");
    form.set(attributeInputName("heatingDistribution"), "UnderfloorHeating");
    form.set(attributeInputName("gasSupply"), "true");
    form.set(attributeInputName("livingAreaM2"), "140.5");

    expect(readAttributes("House", form)).toMatchObject({
      heatingSystem: "OwnBoiler",
      heatingEnergySource: "Gas",
      heatingDistribution: "UnderfloorHeating",
      gasSupply: true,
      livingAreaM2: 140.5,
    });
  });
});

describe("gas supply (Yes/No dropdown)", () => {
  const systems = section("systems");
  const answered = { heatingSystem: "Stove", waterSupply: "Well", sewerage: "None" };

  it("must be answered for the systems section to be complete", () => {
    expect(isSectionComplete(systems, getter(answered), false)).toBe(false);
    expect(isSectionComplete(systems, getter({ ...answered, gasSupply: "false" }), false)).toBe(true);
  });

  it.each([
    ["true", true],
    ["false", false],
  ])("submits %s as a boolean", (raw, expected) => {
    const form = new FormData();
    form.set(attributeInputName("gasSupply"), raw);
    expect(readAttributes("House", form).gasSupply).toBe(expected);
  });

  it("is omitted when not answered", () => {
    const form = new FormData();
    form.set(attributeInputName("gasSupply"), "");
    expect(readAttributes("House", form)).not.toHaveProperty("gasSupply");
  });
});

describe("house condition options", () => {
  it("list the most common conditions first", () => {
    const field = ATTRIBUTE_SCHEMA.House.find((f) => f.name === "houseCondition");
    expect(field && "options" in field ? field.options.slice(0, 3) : []).toEqual([
      "EuroRenovated", "CosmeticRepair", "WhiteStructure",
    ]);
  });
});
