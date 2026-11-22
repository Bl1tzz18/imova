import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { locales } from "@/i18n/config";
import { documentStrings, isSitePath, linkTargets, parseInline } from "@/lib/legal/document";
import { LEGAL_VERSION } from "@/lib/legal/version";
import { PRIVACY, TERMS } from "./index";

// The pages the documents may link to; anything else is a typo (a link that would 404).
const SITE_PATHS = new Set(["/terms", "/privacy", "/privacy#cookies", "/account?tab=privacy"]);

describe.each([
  ["terms", TERMS],
  ["privacy", PRIVACY],
] as const)("%s", (_, byLocale) => {
  it("has the same sections, in the same order, in every language", () => {
    const ro = byLocale.ro.sections.map((s) => s.id);
    for (const locale of locales) {
      expect(byLocale[locale].sections.map((s) => s.id)).toEqual(ro);
    }
    expect(new Set(ro).size).toBe(ro.length);
  });

  it("has the same blocks per section in every language (a translation can't drop a paragraph or a table row)", () => {
    const shape = (locale: (typeof locales)[number]) =>
      byLocale[locale].sections.map((s) =>
        s.blocks.map((b) => (typeof b === "string" ? "p" : "list" in b ? `list:${b.list.length}` : `table:${b.table.columns.length}x${b.table.rows.length}`)),
      );
    for (const locale of locales) {
      expect(shape(locale)).toEqual(shape("ro"));
    }
  });

  it("only links to pages that exist", () => {
    for (const locale of locales) {
      for (const href of linkTargets(byLocale[locale])) {
        if (isSitePath(href)) expect(SITE_PATHS).toContain(href);
        else expect(href).toMatch(/^(https:\/\/|mailto:)/);
      }
    }
  });

  it("has no empty text and no unbalanced marks", () => {
    for (const locale of locales) {
      for (const text of documentStrings(byLocale[locale])) {
        expect(text.trim()).not.toBe("");
        expect(text.split("{{").length).toBe(text.split("}}").length);
        expect((text.match(/\*\*/g) ?? []).length % 2).toBe(0);
        // A mark inside another one (e.g. a placeholder in bold) isn't parsed and would show raw.
        for (const part of parseInline(text)) {
          expect(part.text).not.toMatch(/\{\{|\}\}|\*\*|\]\(/);
        }
      }
    }
  });
});

describe("the version", () => {
  it("is the one the API requires (LegalTerms.CurrentVersion)", () => {
    const csharp = readFileSync(
      fileURLToPath(new URL("../../../../../backend/Imova.Application/Common/LegalTerms.cs", import.meta.url)),
      "utf8",
    );
    expect(csharp).toContain(`CurrentVersion = "${LEGAL_VERSION}"`);
  });
});
