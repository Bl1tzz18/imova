import { describe, expect, it } from "vitest";
import { hasPlaceholders, isSitePath, linkTargets, parseInline, type LegalDocument } from "./document";

describe("parseInline", () => {
  it("splits text, bold, links and placeholders", () => {
    expect(parseInline("Scrie la {{privacy@imova.md}} sau vezi [Cookie-uri](/privacy#cookies). **Important.**")).toEqual([
      { kind: "text", text: "Scrie la " },
      { kind: "placeholder", text: "privacy@imova.md" },
      { kind: "text", text: " sau vezi " },
      { kind: "link", text: "Cookie-uri", href: "/privacy#cookies" },
      { kind: "text", text: ". " },
      { kind: "bold", text: "Important." },
    ]);
  });

  it("leaves plain text alone", () => {
    expect(parseInline("Fără marcaje.")).toEqual([{ kind: "text", text: "Fără marcaje." }]);
    expect(parseInline("")).toEqual([]);
  });
});

describe("a document", () => {
  const doc: LegalDocument = {
    intro: "Vezi [Termenii](/terms).",
    sections: [
      { id: "a", heading: "A", blocks: ["Operator: {{denumirea}}", { list: ["[site](https://datepersonale.md)"] }] },
      { id: "b", heading: "B", blocks: [{ table: { columns: ["X"], rows: [["[cont](/account?tab=privacy)"]] } }] },
    ],
  };

  it("knows when placeholders are left", () => {
    expect(hasPlaceholders(doc)).toBe(true);
    expect(hasPlaceholders({ intro: "Gata.", sections: [] })).toBe(false);
  });

  it("lists every link, tables included", () => {
    expect(linkTargets(doc)).toEqual(["/terms", "https://datepersonale.md", "/account?tab=privacy"]);
  });
});

describe("isSitePath", () => {
  it("tells site paths from outside addresses", () => {
    expect(isSitePath("/privacy#cookies")).toBe(true);
    expect(isSitePath("https://datepersonale.md")).toBe(false);
    expect(isSitePath("mailto:privacy@imova.md")).toBe(false);
    expect(isSitePath("//evil.example")).toBe(false);
  });
});
