import { describe, expect, it } from "vitest";
import { buildListingPayload } from "@/lib/property/formPayload";
import { RENTAL_FIELDS_BY_STEP, rentalFieldsForStep, rentalInputName } from "@/lib/property/rentalFields";

describe("rental fields by step", () => {
  it("asks furnishing and pets on the Details step for a rental", () => {
    expect(rentalFieldsForStep("details", "Rent")).toEqual(["furnishedStatus", "petsAllowed"]);
  });

  it("keeps lease period, deposit, availability and utilities on the Price & terms step", () => {
    expect(rentalFieldsForStep("priceTerms", "Rent")).toEqual([
      "minLeasePeriodMonths", "securityDepositAmount", "availableFrom", "utilitiesIncluded",
    ]);
  });

  it("shows no rental fields on either step for a sale", () => {
    expect(rentalFieldsForStep("details", "Sale")).toEqual([]);
    expect(rentalFieldsForStep("priceTerms", "Sale")).toEqual([]);
  });

  it("asks every rental field exactly once", () => {
    const all = [...RENTAL_FIELDS_BY_STEP.details, ...RENTAL_FIELDS_BY_STEP.priceTerms];
    expect(new Set(all).size).toBe(all.length);
    expect(all.sort()).toEqual([
      "availableFrom", "furnishedStatus", "minLeasePeriodMonths", "petsAllowed", "securityDepositAmount", "utilitiesIncluded",
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

  it("still posts furnishing and pets into rentalDetails, wherever the inputs are rendered", () => {
    const payload = buildListingPayload(
      form("Rent", { furnishedStatus: "PartiallyFurnished", petsAllowed: "true", utilitiesIncluded: "true", minLeasePeriodMonths: "6" }),
    );

    expect(payload.rentalDetails).toEqual({
      furnishedStatus: "PartiallyFurnished",
      petsAllowed: true,
      utilitiesIncluded: true,
      minLeasePeriodMonths: 6,
      securityDepositAmount: null,
      availableFrom: null,
    });
    expect(rentalInputName("furnishedStatus")).toBe("rental.furnishedStatus");
  });

  it("sends no rentalDetails for a sale", () => {
    expect(buildListingPayload(form("Sale", { furnishedStatus: "Furnished", petsAllowed: "true" })).rentalDetails).toBeNull();
  });
});
