import { describe, expect, it } from "vitest";
import { isHighlightSection, keyFacts, listingDetailSections, rentalTermFacts, type DetailFact } from "@/lib/listing/detailSections";
import { DETAIL_LAYOUTS, isAmenitySection, isProximitySection } from "@/lib/property/detailLayouts";
import type { Amenity, Listing, Proximity } from "@/types/listing";

const amenity = (key: string, category: string, types = ["Apartment", "House", "Room"]): Amenity => ({
  id: `id-${key}`,
  key,
  labelRo: key,
  category,
  applicablePropertyTypes: types,
});

const proximity = (key: string): Proximity => ({ id: `p-${key}`, key, labelRo: key, applicablePropertyTypes: ["Apartment"] });

type Input = Pick<Listing, "transactionType" | "property" | "rentalDetails">;

function apartment(overrides: Partial<Input["property"]> = {}, transactionType: "Sale" | "Rent" = "Sale"): Input {
  return {
    transactionType,
    rentalDetails: null,
    property: {
      id: "p1",
      propertyType: "Apartment",
      totalAreaM2: 72,
      yearBuilt: 2015,
      condition: null,
      typeSpecificAttributes: { rooms: 3, floor: 4, totalFloors: 9, buildingMaterial: "Brick" },
      amenities: [],
      proximities: [],
      location: null,
      ...overrides,
    },
  };
}

const fieldsOf = (facts: DetailFact[]) =>
  facts.map((f) => (f.kind === "attribute" ? f.field.name : f.field));

describe("listingDetailSections", () => {
  it("groups what was filled in by the form's sections, in the form's order", () => {
    const sections = listingDetailSections(apartment());

    expect(sections.map((s) => s.id)).toEqual(["structure", "areas"]);
    expect(fieldsOf(sections[0].facts)).toEqual(["yearBuilt", "buildingMaterial", "rooms", "floor"]);
    expect(fieldsOf(sections[1].facts)).toEqual(["totalAreaM2"]);
  });

  it("folds the building's floor count into the floor", () => {
    const floor = listingDetailSections(apartment())[0].facts.find((f) => f.kind === "attribute" && f.field.name === "floor");

    expect(floor).toMatchObject({ value: 4, ofFloors: 9 });
  });

  it("leaves out unanswered fields, and whole sections with nothing in them", () => {
    const sections = listingDetailSections(apartment({ yearBuilt: null, typeSpecificAttributes: { rooms: 2, layout: "" } }));

    expect(sections.map((s) => s.id)).toEqual(["structure", "areas"]);
    expect(fieldsOf(sections[0].facts)).toEqual(["rooms"]);
  });

  it("drops a conditional field whose condition doesn't hold", () => {
    const sections = listingDetailSections(
      apartment({ typeSpecificAttributes: { rooms: 2, heatingSystem: "Convector", heatingEnergySource: "Gas" } }),
    );
    const systems = sections.find((s) => s.id === "systems")!;

    expect(fieldsOf(systems.facts)).toEqual(["heatingSystem"]);
  });

  it("keeps a conditional field whose condition holds", () => {
    const sections = listingDetailSections(
      apartment({ typeSpecificAttributes: { heatingSystem: "OwnBoiler", heatingEnergySource: "Gas", heatingDistribution: "Radiators" } }),
    );

    expect(fieldsOf(sections.find((s) => s.id === "systems")!.facts)).toEqual([
      "heatingSystem",
      "heatingEnergySource",
      "heatingDistribution",
    ]);
  });

  it("keeps a yes/no answered No — it's an answer", () => {
    const systems = listingDetailSections(apartment({ typeSpecificAttributes: { gasSupply: false } })).find((s) => s.id === "systems")!;

    expect(systems.facts).toMatchObject([{ kind: "attribute", value: false }]);
  });

  it("puts each selected amenity in the section it was picked in", () => {
    const sections = listingDetailSections(
      apartment({ amenities: [amenity("furnished", "Comfort"), amenity("intercom", "Security"), amenity("elevator", "General")] }),
    );

    expect(sections.find((s) => s.id === "comfort")!.amenities.map((a) => a.key)).toEqual(["furnished"]);
    expect(sections.find((s) => s.id === "security")!.amenities.map((a) => a.key)).toEqual(["intercom"]);
    expect(sections.find((s) => s.id === "other")!.amenities.map((a) => a.key)).toEqual(["elevator"]);
  });

  it("tells the pick-only sections (highlights) from the fact ones", () => {
    const sections = listingDetailSections(apartment({ amenities: [amenity("furnished", "Comfort")], proximities: [proximity("park")] }));

    expect(sections.filter(isHighlightSection).map((s) => s.id)).toEqual(["comfort", "proximities"]);
    expect(sections.filter((s) => !isHighlightSection(s)).map((s) => s.id)).toEqual(["structure", "areas"]);
  });

  it("shows the proximities in their own section", () => {
    const sections = listingDetailSections(apartment({ proximities: [proximity("school"), proximity("park")] }));

    expect(sections.find((s) => s.id === "proximities")!.proximities.map((p) => p.key)).toEqual(["school", "park"]);
  });

  it("adds the rental rules for a rental only, and keeps the lease terms for the price card", () => {
    const rental = {
      ...apartment({}, "Rent"),
      rentalDetails: {
        petsAllowed: false,
        minLeasePeriodMonths: 12,
        securityDepositAmount: null,
        availableFrom: "2026-11-01",
        utilitiesIncluded: true,
      },
    };

    const sections = listingDetailSections(rental);

    expect(fieldsOf(sections.find((s) => s.id === "rentalRules")!.facts)).toEqual(["petsAllowed"]);
    expect(fieldsOf(rentalTermFacts("Rent", rental.rentalDetails))).toEqual([
      "minLeasePeriodMonths",
      "availableFrom",
      "utilitiesIncluded",
    ]);
    expect(listingDetailSections(apartment()).some((s) => s.id === "rentalRules")).toBe(false);
    expect(rentalTermFacts("Sale", rental.rentalDetails)).toEqual([]);
  });

  it("uses the general condition only where the form asks it (Garage)", () => {
    const garage: Input = {
      transactionType: "Sale",
      rentalDetails: null,
      property: {
        id: "g",
        propertyType: "Garage",
        totalAreaM2: 18,
        yearBuilt: null,
        condition: "Good",
        typeSpecificAttributes: { parkingType: "Garage" },
        amenities: [],
        proximities: [],
        location: null,
      },
    };

    expect(fieldsOf(listingDetailSections(garage)[0].facts)).toEqual(["totalAreaM2", "condition", "parkingType"]);
  });

  it("returns nothing for an unknown property type", () => {
    expect(listingDetailSections(apartment({ propertyType: "Castle" }))).toEqual([]);
  });
});

describe("keyFacts", () => {
  it("picks the headline numbers for the type, only those answered", () => {
    expect(keyFacts(apartment().property)).toEqual([
      { id: "rooms", value: 3 },
      { id: "area", value: 72 },
      { id: "floor", value: 4, ofFloors: 9 },
      { id: "yearBuilt", value: 2015 },
    ]);
    expect(keyFacts(apartment({ yearBuilt: null, typeSpecificAttributes: { rooms: 1 } }).property)).toEqual([
      { id: "rooms", value: 1 },
      { id: "area", value: 72 },
    ]);
  });

  it("uses each type's own headline facts", () => {
    const land = apartment({ propertyType: "Land", totalAreaM2: 1500, yearBuilt: null, typeSpecificAttributes: { plotType: "Construction" } });

    expect(keyFacts(land.property)).toEqual([
      { id: "area", value: 1500 },
      { id: "plotType", value: "Construction" },
    ]);
  });

  it("skips a zero area", () => {
    expect(keyFacts(apartment({ totalAreaM2: 0, yearBuilt: null, typeSpecificAttributes: {} }).property)).toEqual([]);
  });
});

describe("the page's layout assumption", () => {
  // ListingDetails shows every fact section, never collapsed — fine while no type has many.
  it("keeps every property type at five fact sections or fewer", () => {
    for (const layout of Object.values(DETAIL_LAYOUTS)) {
      expect(layout.sections.filter((s) => !isAmenitySection(s) && !isProximitySection(s)).length).toBeLessThanOrEqual(5);
    }
  });
});
