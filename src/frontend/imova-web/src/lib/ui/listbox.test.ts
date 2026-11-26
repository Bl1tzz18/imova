import { describe, expect, it } from "vitest";
import { edgeIndex, filterOptions, stepIndex, typeaheadIndex, type ListOption } from "./listbox";

const options: ListOption[] = [
  { value: "", text: "Alege" },
  { value: "a", text: "Euroreparație" },
  { value: "b", text: "Design individual", disabled: true },
  { value: "c", text: "Reparație cosmetică" },
  { value: "d", text: "Fără reparație" },
  { value: "e", text: "Renovat" },
];

describe("stepIndex", () => {
  it("skips disabled options", () => {
    expect(stepIndex(options, 1, 1)).toBe(3);
    expect(stepIndex(options, 3, -1)).toBe(1);
  });

  it("stays put at either end", () => {
    expect(stepIndex(options, 5, 1)).toBe(5);
    expect(stepIndex(options, 0, -1)).toBe(0);
  });

  it("starts at the first option when nothing is active", () => {
    expect(stepIndex(options, -1, 1)).toBe(0);
  });
});

describe("edgeIndex", () => {
  it("finds the first and last pickable option", () => {
    expect(edgeIndex(options, "first")).toBe(0);
    expect(edgeIndex([{ value: "x", text: "x", disabled: true }, ...options], "first")).toBe(1);
    expect(edgeIndex(options, "last")).toBe(5);
  });

  it("is -1 when nothing can be picked", () => {
    expect(edgeIndex([{ value: "x", text: "x", disabled: true }], "first")).toBe(-1);
  });
});

describe("typeaheadIndex", () => {
  it("jumps to the first option starting with the typed text, ignoring diacritics", () => {
    expect(typeaheadIndex(options, "fa", 0)).toBe(4);
    expect(typeaheadIndex(options, "rep", 0)).toBe(3);
  });

  it("cycles through options with the same first letter when the letter is repeated", () => {
    expect(typeaheadIndex(options, "r", 0)).toBe(3);
    expect(typeaheadIndex(options, "rr", 3)).toBe(5);
    expect(typeaheadIndex(options, "r", 5)).toBe(3);
  });

  it("keeps the current option when a longer prefix still matches it", () => {
    expect(typeaheadIndex(options, "ren", 5)).toBe(5);
  });

  it("never lands on a disabled option and keeps the position when nothing matches", () => {
    expect(typeaheadIndex(options, "d", 1)).toBe(1);
    expect(typeaheadIndex(options, "zz", 3)).toBe(3);
  });
});

describe("filterOptions", () => {
  it("matches anywhere in the text, diacritics ignored", () => {
    expect(filterOptions(options, "reparatie").map((o) => o.value)).toEqual(["a", "c", "d"]);
  });

  it("returns everything for an empty query", () => {
    expect(filterOptions(options, "  ")).toHaveLength(options.length);
  });
});
