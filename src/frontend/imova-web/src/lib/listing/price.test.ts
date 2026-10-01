import { describe, expect, it } from "vitest";
import { convertFromEur, unitPrice } from "./price";

const rates = { EUR: 1, MDL: 0.051, USD: 0.86 };

describe("convertFromEur", () => {
  it("converts with the backend's rates, rounding big amounts to the hundred", () => {
    expect(convertFromEur(82_000, "MDL", rates)).toBe(1_607_800);
    expect(convertFromEur(400, "MDL", rates)).toBe(7_843);
    expect(convertFromEur(86, "USD", rates)).toBe(100);
  });

  it("is null without a rate", () => {
    expect(convertFromEur(82_000, "MDL", null)).toBeNull();
    expect(convertFromEur(82_000, "GBP", rates)).toBeNull();
  });
});

describe("unitPrice", () => {
  const sale = (propertyType: string, totalAreaM2: number, amount = 82_000) => ({
    transactionType: "Sale" as const,
    price: { amount, currency: "EUR", priceEur: amount, isNegotiable: false },
    property: { propertyType, totalAreaM2 },
  });

  it("is per m² for a sale", () => {
    expect(unitPrice(sale("Apartment", 62))).toEqual({ amount: 1_323, currency: "EUR", unit: "m2" });
  });

  it("is per ar (100 m²) for land", () => {
    expect(unitPrice(sale("Land", 1_200, 30_000))).toEqual({ amount: 2_500, currency: "EUR", unit: "ar" });
  });

  it("is left out for a rental or without an area", () => {
    expect(unitPrice({ ...sale("Apartment", 62), transactionType: "Rent" })).toBeNull();
    expect(unitPrice(sale("Room", 0))).toBeNull();
  });
});
