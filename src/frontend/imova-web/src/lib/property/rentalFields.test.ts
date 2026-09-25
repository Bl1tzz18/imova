import { describe, expect, it } from "vitest";
import { buildListingPayload } from "@/lib/property/formPayload";
import { RENTAL_FIELDS_BY_STEP, petsApplyTo, rentalFieldsForStep } from "@/lib/property/rentalFields";

describe("rental fields by step", () => {
  it.each(["Apartment", "House", "Room"])("asks pets on the Details step for a rented %s", (type) => {
    expect(rentalFieldsForStep("details", "Rent", type)).toEqual(["petsAllowed"]);
  });

  it.each(["Land", "Commercial", "Garage"])("doesn't ask pets for a rented %s", (type) => {
    expect(petsApplyTo(type)).toBe(false);
    expect(rentalFieldsForStep("details", "Rent", type)).toEqual([]);
  });

  it("keeps lease period, deposit, availability and utilities on the Price & terms step", () => {
    expect(rentalFieldsForStep("priceTerms", "Rent", "Apartment")).toEqual([
      "minLeasePeriodMonths", "securityDepositAmount", "availableFrom", "utilitiesIncluded",
    ]);
  });

  it("shows no rental fields on either step for a sale", () => {
    expect(rentalFieldsForStep("details", "Sale", "Apartment")).toEqual([]);
    expect(rentalFieldsForStep("priceTerms", "Sale", "Apartment")).toEqual([]);
  });

  it("asks every rental field exactly once, and furnishing isn't one of them", () => {
    const all = [...RENTAL_FIELDS_BY_STEP.details, ...RENTAL_FIELDS_BY_STEP.priceTerms];
    expect(new Set(all).size).toBe(all.length);
    expect([...all].sort()).toEqual([
      "availableFrom", "minLeasePeriodMonths", "petsAllowed", "securityDepositAmount", "utilitiesIncluded",
    ]);
  });
});

describe("rental details payload", () => {
  function form(transactionType: string, rental: Record<string, string>) {
    const data = new FormData();
    data.set("propertyType", "Apartment");
    data.set("transactionType", transactionType);
    for (const [field, value] of Object.entries(rental)) data.set(`rental.${field}`, value);
    return data;
  }

  it.each([
    ["true", true],
    ["false", false],
  ])("posts a %s pets answer as a boolean", (raw, expected) => {
    expect(buildListingPayload(form("Rent", { petsAllowed: raw })).rentalDetails?.petsAllowed).toBe(expected);
  });

  it("posts pets as null when it wasn't asked", () => {
    expect(buildListingPayload(form("Rent", {})).rentalDetails?.petsAllowed).toBeNull();
  });

  it("no longer sends a furnishing level", () => {
    expect(buildListingPayload(form("Rent", { petsAllowed: "true" })).rentalDetails).not.toHaveProperty("furnishedStatus");
  });

  it("posts the lease terms into rentalDetails", () => {
    expect(buildListingPayload(form("Rent", { utilitiesIncluded: "true", minLeasePeriodMonths: "6", petsAllowed: "false" })).rentalDetails).toEqual({
      minLeasePeriodMonths: 6,
      securityDepositAmount: null,
      availableFrom: null,
      utilitiesIncluded: true,
      petsAllowed: false,
    });
  });

  it("sends no rentalDetails for a sale", () => {
    expect(buildListingPayload(form("Sale", { petsAllowed: "true" })).rentalDetails).toBeNull();
  });
});
