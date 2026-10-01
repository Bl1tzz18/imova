import { describe, expect, it } from "vitest";
import { formatPercentChange, priceHistoryRows } from "./priceHistory";

const change = (oldAmount: number, newAmount: number, changePercent: number, changedAt: string) => ({
  oldAmount,
  oldCurrency: "EUR",
  newAmount,
  newCurrency: "EUR",
  changePercent,
  changedAt,
});

describe("priceHistoryRows", () => {
  it("lists the changes newest first and ends with the price at publication", () => {
    const rows = priceHistoryRows(
      {
        startsAtPublication: true,
        changes: [change(82000, 79500, -3, "2026-09-20T10:00:00Z"), change(79500, 81000, 2, "2026-09-28T10:00:00Z")],
      },
      "2026-09-01T10:00:00Z",
    );

    expect(rows).toEqual([
      { kind: "change", amount: 81000, currency: "EUR", date: "2026-09-28T10:00:00Z", changePercent: 2 },
      { kind: "change", amount: 79500, currency: "EUR", date: "2026-09-20T10:00:00Z", changePercent: -3 },
      { kind: "published", amount: 82000, currency: "EUR", date: "2026-09-01T10:00:00Z" },
    ]);
  });

  it("ends with an undated 'earlier' price when older changes were left out", () => {
    const rows = priceHistoryRows({ startsAtPublication: false, changes: [change(500, 450, -10, "2026-09-20T10:00:00Z")] }, null);

    expect(rows.at(-1)).toEqual({ kind: "earlier", amount: 500, currency: "EUR" });
  });

  it("is empty without changes", () => {
    expect(priceHistoryRows({ startsAtPublication: true, changes: [] }, null)).toEqual([]);
  });
});

describe("formatPercentChange", () => {
  it("signs the change with a real minus", () => {
    expect(formatPercentChange(-3)).toBe("−3%");
    expect(formatPercentChange(2)).toBe("+2%");
  });

  it("shows nothing for a change that rounds to zero", () => {
    expect(formatPercentChange(0)).toBeNull();
  });
});
