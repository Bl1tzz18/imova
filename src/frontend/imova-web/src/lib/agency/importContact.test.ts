import { describe, expect, it } from "vitest";
import { IMPORT_CONTACT_EMAIL, importContactMailto } from "./importContact";

describe("writing to IMOVA to import an agency's listings", () => {
  it("opens an email to the one import address, with the subject encoded", () => {
    expect(importContactMailto("Import anunțuri — Casa Ta & Co")).toBe(
      `mailto:${IMPORT_CONTACT_EMAIL}?subject=Import%20anun%C8%9Buri%20%E2%80%94%20Casa%20Ta%20%26%20Co`,
    );
  });

  it("is a real address", () => {
    expect(IMPORT_CONTACT_EMAIL).toMatch(/^[^\s@]+@[^\s@]+\.[^\s@]+$/);
  });
});
