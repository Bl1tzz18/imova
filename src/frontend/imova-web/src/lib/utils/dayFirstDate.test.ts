import { describe, expect, it } from "vitest";
import { digitsAfterEdit, formatDayFirst } from "@/lib/utils/dayFirstDate";

// Replays keystrokes the way the input does: each one edits the currently displayed text.
function type(keys: string, separator = "."): string {
  let digits = "";
  for (const key of keys) {
    const shown = formatDayFirst(digits, separator);
    const raw = key === "⌫" ? shown.slice(0, -1) : shown + key;
    digits = digitsAfterEdit(digits, shown, raw);
  }
  return formatDayFirst(digits, separator);
}

describe("formatDayFirst", () => {
  it.each([
    ["", ""],
    ["2", "2"],
    ["25", "25."],
    ["251", "25.1"],
    ["2512", "25.12."],
    ["25122", "25.12.2"],
    ["25122022", "25.12.2022"],
  ])("shows %s as %s", (digits, expected) => {
    expect(formatDayFirst(digits, ".")).toBe(expected);
  });

  it("uses the locale's separator", () => {
    expect(formatDayFirst("25122022", "/")).toBe("25/12/2022");
  });
});

describe("typing", () => {
  it("inserts the dots automatically", () => {
    expect(type("25122022")).toBe("25.12.2022");
  });

  it("ignores typed separators and extra digits", () => {
    expect(type("25.12.2022")).toBe("25.12.2022");
    expect(type("251220229")).toBe("25.12.2022");
    expect(type("25x")).toBe("25.");
  });

  it("backspace over a trailing dot removes the digit before it too", () => {
    expect(type("25⌫")).toBe("2");
    expect(type("2512⌫")).toBe("25.1");
    expect(type("251⌫")).toBe("25.");
    expect(type("25122022⌫⌫⌫⌫⌫")).toBe("25.1");
  });

  it("works the same with the English slash", () => {
    expect(type("2512⌫", "/")).toBe("25/1");
  });

  it("clears on select-all delete", () => {
    expect(digitsAfterEdit("25122022", "25.12.2022", "")).toBe("");
  });
});
