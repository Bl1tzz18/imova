import { describe, expect, it } from "vitest";
import {
  attentionFix,
  axisDayIndexes,
  change,
  contactRate,
  moldovaToday,
  niceMax,
  parseStatsPreset,
  statsApiQuery,
} from "./stats";

describe("the Statistici period", () => {
  it("reads 7, 30 or 90 days from the address, 30 otherwise", () => {
    expect(parseStatsPreset({ days: "7" })).toBe(7);
    expect(parseStatsPreset({ days: "90" })).toBe(90);
    expect(parseStatsPreset({ days: "12" })).toBe(30);
    expect(parseStatsPreset({})).toBe(30);
  });

  it("asks the API for the last N days ending today", () => {
    expect(statsApiQuery(7, "2026-10-09")).toBe("from=2026-10-03&to=2026-10-09");
    expect(statsApiQuery(30, "2026-03-01")).toBe("from=2026-01-31&to=2026-03-01");
  });

  it("counts days in Moldova, not UTC", () => {
    // 22:30 UTC on 9 October is already 10 October in Chișinău (UTC+3).
    expect(moldovaToday(new Date("2026-10-09T22:30:00Z"))).toBe("2026-10-10");
  });
});

describe("change against the period before", () => {
  it("is a whole percent up or down", () => {
    expect(change(15, 10)).toEqual({ kind: "up", percent: 50 });
    expect(change(5, 10)).toEqual({ kind: "down", percent: 50 });
  });

  it("is 'same' when nothing moved, 'new' when there was nothing before", () => {
    expect(change(10, 10)).toEqual({ kind: "same" });
    expect(change(0, 0)).toEqual({ kind: "same" });
    expect(change(3, 0)).toEqual({ kind: "new" });
  });
});

describe("contactRate", () => {
  it("is contacts per 100 views, one decimal, and nothing without views", () => {
    expect(contactRate(3, 125)).toBe(2.4);
    expect(contactRate(0, 40)).toBe(0);
    expect(contactRate(2, 0)).toBeNull();
  });
});

describe("what fixes a listing that needs attention", () => {
  it("opens the photos step, renews, or opens the listing to edit", () => {
    expect(attentionFix("FewPhotos", "l1")).toEqual({ kind: "edit", href: "/my-listings/l1/edit?step=photos" });
    expect(attentionFix("ExpiringSoon", "l1")).toEqual({ kind: "renew" });
    expect(attentionFix("NoContacts", "l1")).toEqual({ kind: "edit", href: "/my-listings/l1/edit" });
  });
});

describe("axisDayIndexes", () => {
  it("names the first, middle and last day", () => {
    expect(axisDayIndexes(30)).toEqual([0, 14, 29]);
    expect(axisDayIndexes(7)).toEqual([0, 3, 6]);
    expect(axisDayIndexes(2)).toEqual([0, 1]);
    expect(axisDayIndexes(0)).toEqual([]);
  });
});

describe("niceMax", () => {
  it("rounds the top of the scale up to 1, 2 or 5 × 10ⁿ", () => {
    expect(niceMax([0, 0])).toBe(1);
    expect(niceMax([3, 7])).toBe(10);
    expect(niceMax([12])).toBe(20);
    expect(niceMax([430])).toBe(500);
  });
});
